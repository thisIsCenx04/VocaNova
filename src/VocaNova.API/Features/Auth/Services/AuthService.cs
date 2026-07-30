using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
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
using VocaNova.API.Infrastructure.Storage;

namespace VocaNova.API.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private const long MaxAvatarFileBytes = 5 * 1024 * 1024;

    private readonly VocaNovaDbContext _dbContext;
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly IUserProfileCache? _userProfileCache;
    private readonly IKnnTopicRecommendationCache? _knnTopicRecommendationCache;
    private readonly IOtpCodeGenerator _otpCodeGenerator;
    private readonly ISmsProvider _smsProvider;
    private readonly RateLimitSettings _rateLimitSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly IImageStorage? _imageStorage;
    private readonly CloudinarySettings _cloudinarySettings;

    public AuthService(
        VocaNovaDbContext dbContext,
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IGoogleTokenVerifier googleTokenVerifier,
        IOptions<JwtSettings> jwtSettings,
        IUserProfileCache? userProfileCache = null,
        IOtpCodeGenerator? otpCodeGenerator = null,
        ISmsProvider? smsProvider = null,
        IOptions<RateLimitSettings>? rateLimitSettings = null,
        IKnnTopicRecommendationCache? knnTopicRecommendationCache = null,
        IImageStorage? imageStorage = null,
        IOptions<CloudinarySettings>? cloudinarySettings = null)
    {
        _dbContext = dbContext;
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _googleTokenVerifier = googleTokenVerifier;
        _userProfileCache = userProfileCache;
        _knnTopicRecommendationCache = knnTopicRecommendationCache;
        _otpCodeGenerator = otpCodeGenerator ?? new RandomOtpCodeGenerator();
        _smsProvider = smsProvider ?? NullSmsProvider.Instance;
        _rateLimitSettings = rateLimitSettings?.Value ?? new RateLimitSettings();
        _jwtSettings = jwtSettings.Value;
        _imageStorage = imageStorage;
        _cloudinarySettings = cloudinarySettings?.Value ?? new CloudinarySettings();
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

        var otpResult = await ValidateOtpAsync(
            phone,
            userId: null,
            request.OtpCode!,
            cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return PropagateFailure<TokenResponse, OtpVerification>(otpResult);
        }

        var userRole = await _authRepository.FindRoleByNameAsync(UserRole.User, cancellationToken);
        if (userRole is null)
        {
            return Result<TokenResponse>.Fail("Default user role is not configured.");
        }

        var invalidReference = await GetInvalidRegistrationProfileReferenceAsync(request, cancellationToken);
        if (invalidReference is not null)
        {
            return Result<TokenResponse>.Fail($"{invalidReference} is invalid.");
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
                IsPhoneVerified = true,
                PasswordHash = PasswordHelper.Hash(request.Password!),
                UpdatedAt = now,
            },
            new UserProfile
            {
                FullName = displayName,
                UpdatedAt = now,
            },
            await BuildRegistrationLearningProfileAsync(request, now, cancellationToken),
            cancellationToken);

        otpResult.Value!.UserId = user.UserId;
        otpResult.Value.IsUsed = true;
        await _authRepository.SaveChangesAsync(cancellationToken);

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

    public async Task<Result<bool>> DeleteAccountAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.Unauthorized("Invalid user.");
        }

        var now = DateTime.UtcNow;
        user.Status = UserStatus.Deleted;
        user.UpdatedAt = now;

        if (user.UserAuth is not null)
        {
            user.UserAuth.Phone = null;
            user.UserAuth.PasswordHash = null;
            user.UserAuth.IsPhoneVerified = false;
            user.UserAuth.GoogleUid = null;
            user.UserAuth.GoogleEmail = null;
            user.UserAuth.Username = null;
            user.UserAuth.UpdatedAt = now;
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _authRepository.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);
        await RemoveCachedKnnTopicRecommendationsAsync(userId, cancellationToken);
        return Result<bool>.Ok(true);
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

    public async Task<Result<UserProfileDto>> UploadAvatarAsync(
        uint userId,
        UploadAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAvatarFile(request.File);
        if (validationError is not null)
        {
            return Result<UserProfileDto>.Fail(validationError);
        }

        if (_imageStorage is null)
        {
            return Result<UserProfileDto>.Fail("Image storage is not configured.");
        }

        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<UserProfileDto>.Unauthorized("Invalid user.");
        }

        ImageStorageResult uploadResult;
        try
        {
            uploadResult = await _imageStorage.UploadAsync(
                userId,
                request.File!,
                _cloudinarySettings.AvatarFolder,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Result<UserProfileDto>.Fail(exception.Message);
        }

        await _authRepository.UpdateUserProfileAsync(
            user,
            user.UserProfile?.FullName ?? string.Empty,
            uploadResult.Url,
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
        await RemoveCachedKnnTopicRecommendationsAsync(userId, cancellationToken);

        return Result<UserProfileDto>.Ok(MapUserProfile(user));
    }

    public async Task<Result<OtpSendResponse>> SendOtpAsync(
        OtpSendRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var purpose = NormalizeOtpPurpose(request.Purpose);
        if (!OtpPurpose.All.Contains(purpose))
        {
            return Result<OtpSendResponse>.Fail("Purpose must be register, verify, or reset.");
        }

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
        uint? otpUserId = null;
        if (purpose == OtpPurpose.Reset)
        {
            if (user?.UserAuth?.PasswordHash is null || user.Status == UserStatus.Deleted)
            {
                return Result<OtpSendResponse>.NotFound("User not found.");
            }

            otpUserId = user.UserId;
        }
        else if (user is not null && user.Status != UserStatus.Deleted)
        {
            return Result<OtpSendResponse>.Conflict("Phone already exists.");
        }

        var otpCode = _otpCodeGenerator.Generate();

        await _authRepository.CreateOtpAsync(
            new OtpVerification
            {
                UserId = otpUserId,
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
        var result = await ValidateOtpAsync(phone, userId: null, request.OtpCode!, cancellationToken);
        if (!result.IsSuccess)
        {
            return PropagateFailure<OtpVerifyResponse, OtpVerification>(result);
        }

        result.Value!.IsUsed = true;
        await _authRepository.SaveChangesAsync(cancellationToken);

        return Result<OtpVerifyResponse>.Ok(new OtpVerifyResponse(true));
    }

    public async Task<Result<OtpSendResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.UserAuth?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return Result<OtpSendResponse>.NotFound("User not found.");
        }

        return await SendOtpAsync(new OtpSendRequest(phone, OtpPurpose.Reset), cancellationToken);
    }

    public async Task<Result<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var user = await _authRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.UserAuth?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.Unauthorized("Invalid phone or OTP.");
        }

        var otpResult = await ValidateOtpAsync(
            phone,
            user.UserId,
            request.OtpCode!,
            cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return PropagateFailure<bool, OtpVerification>(otpResult);
        }

        var now = DateTime.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _authRepository.UpdatePasswordAsync(
            user,
            PasswordHelper.Hash(request.NewPassword!),
            now,
            cancellationToken);

        var otp = otpResult.Value!;
        otp.IsUsed = true;
        await _authRepository.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        uint userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.FindByIdAsync(userId, cancellationToken);
        if (user?.UserAuth?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.Unauthorized("Invalid user.");
        }

        if (!PasswordHelper.Verify(request.CurrentPassword!, user.UserAuth.PasswordHash))
        {
            return Result<bool>.Unauthorized("Current password is incorrect.");
        }

        await _authRepository.UpdatePasswordAsync(
            user,
            PasswordHelper.Hash(request.NewPassword!),
            DateTime.UtcNow,
            cancellationToken);

        return Result<bool>.Ok(true);
    }

    private async Task<Result<OtpVerification>> ValidateOtpAsync(
        string phone,
        uint? userId,
        string otpCode,
        CancellationToken cancellationToken)
    {
        var otp = await _authRepository.FindLatestOtpByPhoneAndUserAsync(
            phone,
            userId,
            cancellationToken);
        if (otp is null)
        {
            return Result<OtpVerification>.Unauthorized("Invalid OTP.");
        }

        var now = DateTime.UtcNow;
        if (otp.ExpiresAt <= now)
        {
            return Result<OtpVerification>.Unauthorized("OTP has expired.");
        }

        if (otp.IsUsed)
        {
            return Result<OtpVerification>.Conflict("OTP has already been used.");
        }

        if (otp.VerifyAttemptCount >= AppSettings.OtpMaxVerifyAttempts)
        {
            return Result<OtpVerification>.TooManyRequests("Maximum OTP verify attempts exceeded.");
        }

        otp.VerifyAttemptCount++;
        if (otp.OtpCode != otpCode)
        {
            await _authRepository.SaveChangesAsync(cancellationToken);
            return Result<OtpVerification>.Unauthorized("Invalid OTP.");
        }

        return Result<OtpVerification>.Ok(otp);
    }

    private static string NormalizeOtpPurpose(string? purpose)
    {
        return string.IsNullOrWhiteSpace(purpose)
            ? OtpPurpose.Verify
            : purpose.Trim().ToLowerInvariant();
    }

    private static Result<TOut> PropagateFailure<TOut, TIn>(Result<TIn> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Result<TOut>.Unauthorized(result.Error!),
            StatusCodes.Status403Forbidden => Result<TOut>.Forbidden(result.Error!),
            StatusCodes.Status404NotFound => Result<TOut>.NotFound(result.Error!),
            StatusCodes.Status409Conflict => Result<TOut>.Conflict(result.Error!),
            StatusCodes.Status429TooManyRequests => Result<TOut>.TooManyRequests(result.Error!),
            _ => Result<TOut>.Fail(result.Error!),
        };
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

    private async Task<string?> GetInvalidRegistrationProfileReferenceAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RegionId.HasValue
            && !await _authRepository.ActiveRegionExistsAsync(request.RegionId.Value, cancellationToken))
        {
            return nameof(RegisterRequest.RegionId);
        }

        if (request.OccupationId.HasValue
            && !await _authRepository.ActiveOccupationExistsAsync(request.OccupationId.Value, cancellationToken))
        {
            return nameof(RegisterRequest.OccupationId);
        }

        if (request.EducationLevelId.HasValue
            && !await _authRepository.ActiveEducationLevelExistsAsync(request.EducationLevelId.Value, cancellationToken))
        {
            return nameof(RegisterRequest.EducationLevelId);
        }

        return null;
    }

    /// <summary>
    /// Seeds the KNN profile vector at sign-up. The schema has no column for the raw date of
    /// birth, so only the derived age range is persisted; the user can correct it later from
    /// the profile screen. Returns <c>null</c> when the caller supplied nothing, so registration
    /// without optional fields keeps its previous behaviour.
    /// </summary>
    private async Task<UserLearningProfile?> BuildRegistrationLearningProfileAsync(
        RegisterRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        uint? ageRangeId = null;
        if (request.DateOfBirth.HasValue)
        {
            var age = AgeHelper.CalculateAge(request.DateOfBirth.Value, DateOnly.FromDateTime(now));
            ageRangeId = await _authRepository.ResolveAgeRangeIdByAgeAsync(age, cancellationToken);
        }

        if (ageRangeId is null
            && request.RegionId is null
            && request.OccupationId is null
            && request.EducationLevelId is null)
        {
            return null;
        }

        return new UserLearningProfile
        {
            AgeRangeId = ageRangeId,
            RegionId = request.RegionId,
            OccupationId = request.OccupationId,
            EducationLevelId = request.EducationLevelId,
            CreatedAt = now,
            UpdatedAt = now,
        };
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

    private static string? ValidateAvatarFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Avatar file is required.";
        }

        if (file.Length > MaxAvatarFileBytes)
        {
            return "Avatar file must be 5MB or smaller.";
        }

        if (!AllowedAvatarContentTypes.Contains(file.ContentType))
        {
            return "Avatar MIME type must be one of: image/jpeg, image/png, image/webp.";
        }

        return null;
    }

    private static readonly IReadOnlySet<string> AllowedAvatarContentTypes = new HashSet<string>(
        new[] { "image/jpeg", "image/png", "image/webp" },
        StringComparer.OrdinalIgnoreCase);

    private async Task RemoveCachedKnnTopicRecommendationsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_knnTopicRecommendationCache is not null)
        {
            await _knnTopicRecommendationCache.RemoveAsync(userId, cancellationToken);
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
