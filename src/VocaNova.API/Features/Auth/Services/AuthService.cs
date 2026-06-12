using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Otp;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.API.Infrastructure.RateLimiting;
using VocaNova.API.Infrastructure.Sms;

namespace VocaNova.API.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly IUserProfileCache? _userProfileCache;
    private readonly IOtpCodeGenerator _otpCodeGenerator;
    private readonly ISmsProvider _smsProvider;
    private readonly RateLimitSettings _rateLimitSettings;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        VocaNovaDbContext dbContext,
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IGoogleTokenVerifier googleTokenVerifier,
        IOptions<JwtSettings> jwtSettings,
        IUserProfileCache? userProfileCache = null,
        IOtpCodeGenerator? otpCodeGenerator = null,
        ISmsProvider? smsProvider = null,
        IOptions<RateLimitSettings>? rateLimitSettings = null)
    {
        _dbContext = dbContext;
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _googleTokenVerifier = googleTokenVerifier;
        _userProfileCache = userProfileCache;
        _otpCodeGenerator = otpCodeGenerator ?? new RandomOtpCodeGenerator();
        _smsProvider = smsProvider ?? NullSmsProvider.Instance;
        _rateLimitSettings = rateLimitSettings?.Value ?? new RateLimitSettings();
        _jwtSettings = jwtSettings.Value;
        _jwtSettings.Validate();
    }

    public async Task<Result<TokenResponse>> RegisterAsync(
        RegisterRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var displayName = request.DisplayName!.Trim();

        var existingUser = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        if (existingUser is not null && existingUser.Status != UserStatus.Deleted)
        {
            return Result<TokenResponse>.Conflict("Phone already exists.");
        }

        var userRole = await _authRepository.FindRoleByNameAsync(UserRole.User, cancellationToken);
        if (userRole is null)
        {
            return Result<TokenResponse>.Fail("Default user role is not configured.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var user = new User
        {
            RoleId = userRole.RoleId,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _authRepository.CreateUserAsync(
            user,
            new UserAuth
            {
                Phone = phone,
                IsPhoneVerified = false,
                PasswordHash = PasswordHelper.Hash(request.Password!),
                UpdatedAt = now,
            },
            new UserProfile
            {
                FullName = displayName,
                UpdatedAt = now,
            },
            cancellationToken: cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.UserId, UserRole.User);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _authRepository.CreateRefreshTokenAsync(
            new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = TokenHelper.HashSha256(refreshToken),
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedAt = now,
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result<TokenResponse>.Ok(new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: _jwtSettings.AccessTokenMinutes * 60));
    }

    public async Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);

        if (user?.UserAuth?.PasswordHash is null
            || !PasswordHelper.Verify(request.Password!, user.UserAuth.PasswordHash))
        {
            return Result<TokenResponse>.Unauthorized("Invalid phone or password.");
        }

        if (user.Status == UserStatus.Locked)
        {
            return Result<TokenResponse>.Forbidden("User account is locked.");
        }

        if (user.Status == UserStatus.Deleted)
        {
            return Result<TokenResponse>.Unauthorized("Invalid phone or password.");
        }

        var roleName = user.Role.RoleName;
        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        var accessToken = _jwtTokenService.GenerateAccessToken(user.UserId, roleName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _authRepository.CreateRefreshTokenAsync(
            new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = TokenHelper.HashSha256(refreshToken),
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedAt = now,
            },
            cancellationToken);

        return Result<TokenResponse>.Ok(new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: _jwtSettings.AccessTokenMinutes * 60));
    }

    public async Task<Result<TokenResponse>> GoogleLoginAsync(
        GoogleLoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleTokenVerifier.VerifyAsync(request.IdToken!, cancellationToken);
        if (googleUser is null)
        {
            return Result<TokenResponse>.Unauthorized("Invalid Google id token.");
        }

        var user = await _authRepository.FindByGoogleUidAsync(googleUser.Subject, cancellationToken);
        if (user is not null)
        {
            return await SignInUserAsync(user, deviceInfo, ipAddress, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(googleUser.Email))
        {
            var emailOwner = await _authRepository.FindByGoogleEmailAsync(googleUser.Email, cancellationToken);
            if (emailOwner is not null)
            {
                return Result<TokenResponse>.Conflict("Google email already exists.");
            }
        }

        var userRole = await _authRepository.FindRoleByNameAsync(UserRole.User, cancellationToken);
        if (userRole is null)
        {
            return Result<TokenResponse>.Fail("Default user role is not configured.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var newUser = new User
        {
            RoleId = userRole.RoleId,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _authRepository.CreateUserAsync(
            newUser,
            new UserAuth
            {
                GoogleUid = googleUser.Subject,
                GoogleEmail = googleUser.Email,
                IsPhoneVerified = false,
                UpdatedAt = now,
            },
            new UserProfile
            {
                FullName = ResolveGoogleDisplayName(googleUser),
                AvatarUrl = googleUser.Picture,
                UpdatedAt = now,
            },
            cancellationToken: cancellationToken);

        var tokenResponse = await CreateAndStoreTokenResponseAsync(
            newUser,
            UserRole.User,
            now,
            deviceInfo,
            ipAddress,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result<TokenResponse>.Ok(tokenResponse);
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenHelper.HashSha256(request.RefreshToken!);
        var refreshToken = await _authRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            return Result<TokenResponse>.Unauthorized("Invalid refresh token.");
        }

        if (refreshToken.RevokedAt is not null)
        {
            return Result<TokenResponse>.Unauthorized("Refresh token has been revoked.");
        }

        var now = DateTime.UtcNow;
        if (refreshToken.ExpiresAt <= now)
        {
            return Result<TokenResponse>.Unauthorized("Refresh token has expired.");
        }

        if (refreshToken.User.Status == UserStatus.Locked)
        {
            return Result<TokenResponse>.Forbidden("User account is locked.");
        }

        if (refreshToken.User.Status == UserStatus.Deleted)
        {
            return Result<TokenResponse>.Unauthorized("Invalid refresh token.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _authRepository.RevokeRefreshTokenAsync(refreshToken, now, cancellationToken);
        var tokenResponse = await CreateAndStoreTokenResponseAsync(
            refreshToken.User,
            refreshToken.User.Role.RoleName,
            now,
            deviceInfo,
            ipAddress,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result<TokenResponse>.Ok(tokenResponse);
    }

    public async Task<Result<bool>> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<bool>.Unauthorized("Invalid refresh token.");
        }

        var tokenHash = TokenHelper.HashSha256(request.RefreshToken);
        var revoked = await _authRepository.RevokeTokenAsync(tokenHash, DateTime.UtcNow, cancellationToken);
        return revoked
            ? Result<bool>.Ok(true)
            : Result<bool>.Unauthorized("Invalid refresh token.");
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var cachedProfile = _userProfileCache is null
            ? null
            : await _userProfileCache.GetAsync(userId, cancellationToken);
        if (cachedProfile is not null)
        {
            return Result<UserProfileDto>.Ok(cachedProfile);
        }

        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<UserProfileDto>.Unauthorized("Invalid user.");
        }

        var profile = MapUserProfile(user);
        if (_userProfileCache is not null)
        {
            await _userProfileCache.SetAsync(profile, cancellationToken);
        }

        return Result<UserProfileDto>.Ok(profile);
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(
        uint userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<UserProfileDto>.Unauthorized("Invalid user.");
        }

        await _authRepository.UpdateUserProfileAsync(
            user,
            request.DisplayName!.Trim(),
            string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            DateTime.UtcNow,
            cancellationToken);

        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<UserProfileDto>.Ok(MapUserProfile(user));
    }

    public async Task<Result<UserProfileDto>> UpdateLearningProfileAsync(
        uint userId,
        UpdateLearningProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var invalidReference = await GetInvalidLearningProfileReferenceAsync(request, cancellationToken);
        if (invalidReference is not null)
        {
            return Result<UserProfileDto>.Fail($"{invalidReference} is invalid.");
        }

        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<UserProfileDto>.Unauthorized("Invalid user.");
        }

        await _authRepository.UpsertLearningProfileAsync(
            user,
            request.AgeRangeId,
            request.RegionId,
            request.OccupationId,
            request.EducationLevelId,
            request.LearningPurposeId,
            DateTime.UtcNow,
            cancellationToken);

        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<UserProfileDto>.Ok(MapUserProfile(user));
    }

    public async Task<Result<OtpSendResponse>> SendOtpAsync(
        OtpSendRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var now = DateTime.UtcNow;
        var rateLimitWindowStart = now.AddSeconds(-_rateLimitSettings.RetryAfterSeconds);
        var recentOtp = await _authRepository.FindLatestOtpByPhoneSinceAsync(
            phone,
            rateLimitWindowStart,
            cancellationToken);
        if (recentOtp is not null)
        {
            return Result<OtpSendResponse>.TooManyRequests("OTP request rate limit exceeded.");
        }

        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        var otpCode = _otpCodeGenerator.Generate();

        await _authRepository.CreateOtpAsync(
            new OtpVerification
            {
                UserId = user?.UserId,
                Phone = phone,
                OtpCode = otpCode,
                IsUsed = false,
                Status = OtpStatus.Active,
                VerifyAttemptCount = 0,
                ExpiresAt = now.AddMinutes(AppSettings.OtpTtlMinutes),
                CreatedAt = now,
            },
            cancellationToken);

        await _smsProvider.SendOtpAsync(phone, otpCode, cancellationToken);

        return Result<OtpSendResponse>.Ok(new OtpSendResponse(AppSettings.OtpTtlMinutes * 60));
    }

    public async Task<Result<OtpVerifyResponse>> VerifyOtpAsync(
        OtpVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var otp = await _authRepository.FindLatestOtpByPhoneAsync(phone, cancellationToken);
        if (otp is null)
        {
            return Result<OtpVerifyResponse>.Unauthorized("Invalid OTP.");
        }

        var now = DateTime.UtcNow;
        if (otp.ExpiresAt <= now)
        {
            return Result<OtpVerifyResponse>.Unauthorized("OTP has expired.");
        }

        if (otp.IsUsed)
        {
            return Result<OtpVerifyResponse>.Conflict("OTP has already been used.");
        }

        if (otp.VerifyAttemptCount >= AppSettings.OtpMaxVerifyAttempts)
        {
            return Result<OtpVerifyResponse>.TooManyRequests("Maximum OTP verify attempts exceeded.");
        }

        otp.VerifyAttemptCount++;

        if (otp.OtpCode != request.OtpCode)
        {
            await _authRepository.SaveChangesAsync(cancellationToken);
            return Result<OtpVerifyResponse>.Unauthorized("Invalid OTP.");
        }

        otp.IsUsed = true;
        await _authRepository.SaveChangesAsync(cancellationToken);

        return Result<OtpVerifyResponse>.Ok(new OtpVerifyResponse(true));
    }

    public async Task<Result<OtpSendResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<OtpSendResponse>.NotFound("User not found.");
        }

        return await SendOtpAsync(new OtpSendRequest(phone, "reset"), cancellationToken);
    }

    public async Task<Result<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.UserAuth is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.Unauthorized("Invalid phone or OTP.");
        }

        var otp = await _authRepository.FindLatestOtpByPhoneAsync(phone, cancellationToken);
        if (otp is null)
        {
            return Result<bool>.Unauthorized("Invalid phone or OTP.");
        }

        var now = DateTime.UtcNow;
        if (otp.ExpiresAt <= now)
        {
            return Result<bool>.Unauthorized("OTP has expired.");
        }

        if (otp.IsUsed)
        {
            return Result<bool>.Conflict("OTP has already been used.");
        }

        if (otp.VerifyAttemptCount >= AppSettings.OtpMaxVerifyAttempts)
        {
            return Result<bool>.TooManyRequests("Maximum OTP verify attempts exceeded.");
        }

        otp.VerifyAttemptCount++;

        if (otp.OtpCode != request.OtpCode)
        {
            await _authRepository.SaveChangesAsync(cancellationToken);
            return Result<bool>.Unauthorized("Invalid phone or OTP.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _authRepository.UpdatePasswordAsync(
            user,
            PasswordHelper.Hash(request.NewPassword!),
            now,
            cancellationToken);

        otp.IsUsed = true;
        await _authRepository.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    private async Task<Result<TokenResponse>> SignInUserAsync(
        User user,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (user.Status == UserStatus.Locked)
        {
            return Result<TokenResponse>.Forbidden("User account is locked.");
        }

        if (user.Status == UserStatus.Deleted)
        {
            return Result<TokenResponse>.Unauthorized("Invalid Google account.");
        }

        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        var tokenResponse = await CreateAndStoreTokenResponseAsync(
            user,
            user.Role.RoleName,
            now,
            deviceInfo,
            ipAddress,
            cancellationToken);

        return Result<TokenResponse>.Ok(tokenResponse);
    }

    private async Task<TokenResponse> CreateAndStoreTokenResponseAsync(
        User user,
        string roleName,
        DateTime now,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user.UserId, roleName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _authRepository.CreateRefreshTokenAsync(
            new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = TokenHelper.HashSha256(refreshToken),
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedAt = now,
            },
            cancellationToken);

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: _jwtSettings.AccessTokenMinutes * 60);
    }

    private static string ResolveGoogleDisplayName(GoogleUserInfo googleUser)
    {
        return !string.IsNullOrWhiteSpace(googleUser.Name)
            ? googleUser.Name
            : googleUser.Email ?? "Google User";
    }

    private async Task<string?> GetInvalidLearningProfileReferenceAsync(
        UpdateLearningProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AgeRangeId.HasValue
            && !await _authRepository.ActiveAgeRangeExistsAsync(request.AgeRangeId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileRequest.AgeRangeId);
        }

        if (request.RegionId.HasValue
            && !await _authRepository.ActiveRegionExistsAsync(request.RegionId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileRequest.RegionId);
        }

        if (request.OccupationId.HasValue
            && !await _authRepository.ActiveOccupationExistsAsync(request.OccupationId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileRequest.OccupationId);
        }

        if (request.EducationLevelId.HasValue
            && !await _authRepository.ActiveEducationLevelExistsAsync(request.EducationLevelId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileRequest.EducationLevelId);
        }

        if (request.LearningPurposeId.HasValue
            && !await _authRepository.ActiveLearningPurposeExistsAsync(request.LearningPurposeId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileRequest.LearningPurposeId);
        }

        return null;
    }

    private async Task RemoveCachedProfileAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_userProfileCache is not null)
        {
            await _userProfileCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static UserProfileDto MapUserProfile(User user)
    {
        var learningProfile = user.UserLearningProfile is null
            ? null
            : new LearningProfileDto(
                user.UserLearningProfile.AgeRangeId,
                user.UserLearningProfile.RegionId,
                user.UserLearningProfile.OccupationId,
                user.UserLearningProfile.EducationLevelId,
                user.UserLearningProfile.LearningPurposeId);

        return new UserProfileDto(
            user.UserId,
            user.UserAuth?.Phone,
            user.UserProfile?.FullName ?? string.Empty,
            user.UserProfile?.AvatarUrl,
            user.Role.RoleName,
            user.Status,
            learningProfile);
    }

    private sealed class NullSmsProvider : ISmsProvider
    {
        public static readonly NullSmsProvider Instance = new();

        private NullSmsProvider()
        {
        }

        public Task SendOtpAsync(string phone, string otpCode, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
