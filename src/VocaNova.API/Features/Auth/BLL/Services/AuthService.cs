using Microsoft.Extensions.Options;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Services.IServices;

namespace VocaNova.API.Features.Auth.BLL.Services;

public sealed class AuthService : IAuthService
{
    private const long MaxAvatarFileBytes = 5 * 1024 * 1024;

    private readonly IAuthAccountRepository _accountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IApplicationTransactionManager _transactionManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleIdentityProvider _googleIdentityProvider;
    private readonly IUserProfileCache? _userProfileCache;
    private readonly IKnnTopicRecommendationCache? _knnTopicRecommendationCache;
    private readonly IOtpCodeGenerator _otpCodeGenerator;
    private readonly ISmsSender _smsSender;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IAvatarStorage? _avatarStorage;
    private readonly AuthTokenOptions _tokenOptions;
    private readonly AuthRateLimitOptions _rateLimitOptions;

    public AuthService(
        IAuthAccountRepository accountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOtpRepository otpRepository,
        IApplicationTransactionManager transactionManager,
        IJwtTokenService jwtTokenService,
        IGoogleIdentityProvider googleIdentityProvider,
        IPasswordHasher passwordHasher,
        IRefreshTokenHasher refreshTokenHasher,
        IOptions<AuthTokenOptions> tokenOptions,
        IUserProfileCache? userProfileCache = null,
        IOtpCodeGenerator? otpCodeGenerator = null,
        ISmsSender? smsSender = null,
        IOptions<AuthRateLimitOptions>? rateLimitOptions = null,
        IKnnTopicRecommendationCache? knnTopicRecommendationCache = null,
        IAvatarStorage? avatarStorage = null)
    {
        _accountRepository = accountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _otpRepository = otpRepository;
        _transactionManager = transactionManager;
        _jwtTokenService = jwtTokenService;
        _googleIdentityProvider = googleIdentityProvider;
        _passwordHasher = passwordHasher;
        _refreshTokenHasher = refreshTokenHasher;
        _tokenOptions = tokenOptions.Value;
        _userProfileCache = userProfileCache;
        _otpCodeGenerator = otpCodeGenerator ?? NullOtpCodeGenerator.Instance;
        _smsSender = smsSender ?? NullSmsSender.Instance;
        _rateLimitOptions = rateLimitOptions?.Value ?? new AuthRateLimitOptions();
        _knnTopicRecommendationCache = knnTopicRecommendationCache;
        _avatarStorage = avatarStorage;
    }

    public async Task<AuthOperationResult<AuthTokenPair>> RegisterAsync(
        RegisterCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var displayName = command.DisplayName!.Trim();

        var existingUser = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);
        if (existingUser is not null && existingUser.Status != UserStatus.Deleted)
        {
            return AuthOperationResult<AuthTokenPair>.Conflict("Phone already exists.");
        }

        var otpResult = await ValidateOtpAsync(phone, userId: null, command.OtpCode!, persistInvalidAttempt: true, cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return PropagateFailure<AuthTokenPair, OtpRecord>(otpResult);
        }

        var userRole = await _accountRepository.FindRoleByNameAsync(UserRole.User, cancellationToken);
        if (userRole is null)
        {
            return AuthOperationResult<AuthTokenPair>.ValidationFailure("Default user role is not configured.");
        }

        var invalidReference = await GetInvalidRegistrationProfileReferenceAsync(command, cancellationToken);
        if (invalidReference is not null)
        {
            return AuthOperationResult<AuthTokenPair>.ValidationFailure($"{invalidReference} is invalid.");
        }

        var now = DateTime.UtcNow;
        var learningProfile = await BuildRegistrationLearningProfileAsync(command, now, cancellationToken);
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);

        await _accountRepository.StageCreateAsync(
            new CreateAuthAccount(
                userRole.RoleId,
                UserStatus.Active,
                phone,
                _passwordHasher.Hash(command.Password!),
                IsPhoneVerified: true,
                GoogleSubject: null,
                GoogleEmail: null,
                displayName,
                AvatarUrl: null,
                learningProfile,
                now),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);

        var createdAccount = await _accountRepository.FindByPhoneAsync(phone, cancellationToken)
            ?? throw new InvalidOperationException("Created account could not be reloaded.");
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        await _otpRepository.StageUsedAsync(otpResult.Value!, createdAccount.UserId, now, cancellationToken);
        await _refreshTokenRepository.StageCreateAsync(
            CreateRefreshToken(createdAccount.UserId, refreshToken, now, signInContext),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<AuthTokenPair>.Success(CreateTokenPair(createdAccount.UserId, UserRole.User, refreshToken));
    }

    public async Task<AuthOperationResult<AuthTokenPair>> LoginAsync(
        LoginCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var user = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);

        if (user?.PasswordHash is null || !_passwordHasher.Verify(command.Password!, user.PasswordHash))
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Invalid phone or password.");
        }

        var statusResult = ValidateSignInStatus<AuthTokenPair>(
            user,
            deletedMessage: "Invalid phone or password.",
            lockedMessage: "User account is locked.");
        if (statusResult is not null)
        {
            return statusResult;
        }

        return await SignInAccountAsync(user, signInContext, cancellationToken);
    }

    public async Task<AuthOperationResult<AuthTokenPair>> GoogleLoginAsync(
        GoogleLoginCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleIdentityProvider.VerifyAsync(command.IdToken!, cancellationToken);
        if (googleUser is null)
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Invalid Google id token.");
        }

        var user = await _accountRepository.FindByGoogleSubjectAsync(googleUser.Subject, cancellationToken);
        if (user is not null)
        {
            var statusResult = ValidateSignInStatus<AuthTokenPair>(
                user,
                deletedMessage: "Invalid Google account.",
                lockedMessage: "User account is locked.");
            return statusResult ?? await SignInAccountAsync(user, signInContext, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(googleUser.Email))
        {
            var emailOwner = await _accountRepository.FindByGoogleEmailAsync(googleUser.Email, cancellationToken);
            if (emailOwner is not null)
            {
                return AuthOperationResult<AuthTokenPair>.Conflict("Google email already exists.");
            }
        }

        var userRole = await _accountRepository.FindRoleByNameAsync(UserRole.User, cancellationToken);
        if (userRole is null)
        {
            return AuthOperationResult<AuthTokenPair>.ValidationFailure("Default user role is not configured.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _accountRepository.StageCreateAsync(
            new CreateAuthAccount(
                userRole.RoleId,
                UserStatus.Active,
                Phone: null,
                PasswordHash: null,
                IsPhoneVerified: false,
                googleUser.Subject,
                googleUser.Email,
                ResolveGoogleDisplayName(googleUser),
                googleUser.Picture,
                LearningProfile: null,
                now),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);

        var createdAccount = await _accountRepository.FindByGoogleSubjectAsync(googleUser.Subject, cancellationToken)
            ?? throw new InvalidOperationException("Created Google account could not be reloaded.");
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        await _refreshTokenRepository.StageCreateAsync(
            CreateRefreshToken(createdAccount.UserId, refreshToken, now, signInContext),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<AuthTokenPair>.Success(CreateTokenPair(createdAccount.UserId, UserRole.User, refreshToken));
    }

    public async Task<AuthOperationResult<AuthTokenPair>> RefreshTokenAsync(
        RefreshTokenCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenHasher.Hash(command.RefreshToken!);
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var refreshToken = await _refreshTokenRepository.FindForUpdateByHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Invalid refresh token.");
        }

        if (refreshToken.RevokedAt is not null)
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Refresh token has been revoked.");
        }

        var now = DateTime.UtcNow;
        if (refreshToken.ExpiresAt <= now)
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Refresh token has expired.");
        }

        if (refreshToken.UserStatus == UserStatus.Locked)
        {
            return AuthOperationResult<AuthTokenPair>.Forbidden("User account is locked.");
        }

        if (refreshToken.UserStatus == UserStatus.Deleted)
        {
            return AuthOperationResult<AuthTokenPair>.Unauthorized("Invalid refresh token.");
        }

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        await _refreshTokenRepository.StageRevokeAsync(tokenHash, now, cancellationToken);
        await _refreshTokenRepository.StageCreateAsync(
            CreateRefreshToken(refreshToken.UserId, newRefreshToken, now, signInContext),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<AuthTokenPair>.Success(
            CreateTokenPair(refreshToken.UserId, refreshToken.RoleName, newRefreshToken));
    }

    public async Task<AuthOperationResult<bool>> LogoutAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid refresh token.");
        }

        var tokenHash = _refreshTokenHasher.Hash(command.RefreshToken);
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var revoked = await _refreshTokenRepository.StageRevokeAsync(tokenHash, DateTime.UtcNow, cancellationToken);
        if (!revoked)
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid refresh token.");
        }

        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthOperationResult<bool>.Success(true);
    }

    public async Task<AuthOperationResult<UserProfile>> GetProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var cachedProfile = _userProfileCache is null ? null : await _userProfileCache.GetAsync(userId, cancellationToken);
        if (cachedProfile is not null)
        {
            return AuthOperationResult<UserProfile>.Success(cachedProfile);
        }

        var profile = await _accountRepository.GetProfileAsync(userId, cancellationToken);
        if (profile is null || profile.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<UserProfile>.Unauthorized("Invalid user.");
        }

        if (_userProfileCache is not null)
        {
            await _userProfileCache.SetAsync(profile, cancellationToken);
        }

        return AuthOperationResult<UserProfile>.Success(profile);
    }

    public async Task<AuthOperationResult<bool>> DeleteAccountAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.FindByIdAsync(userId, cancellationToken);
        if (account is null || account.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid user.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        if (!await _accountRepository.StageSoftDeleteAsync(userId, now, cancellationToken))
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid user.");
        }

        await _refreshTokenRepository.StageRevokeAllAsync(userId, now, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await RemoveCachedProfileAsync(userId, cancellationToken);
        await RemoveCachedKnnTopicRecommendationsAsync(userId, cancellationToken);
        return AuthOperationResult<bool>.Success(true);
    }

    public async Task<AuthOperationResult<UserProfile>> UpdateProfileAsync(
        uint userId,
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.FindByIdAsync(userId, cancellationToken);
        if (account is null || account.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<UserProfile>.Unauthorized("Invalid user.");
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var profile = await _accountRepository.UpdateProfileAsync(userId, command, DateTime.UtcNow, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return AuthOperationResult<UserProfile>.Success(profile!);
    }

    public async Task<AuthOperationResult<UserProfile>> UploadAvatarAsync(
        uint userId,
        UploadAvatarCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAvatarFile(command.Content);
        if (validationError is not null)
        {
            return AuthOperationResult<UserProfile>.ValidationFailure(validationError);
        }

        if (_avatarStorage is null)
        {
            return AuthOperationResult<UserProfile>.ValidationFailure("Image storage is not configured.");
        }

        var account = await _accountRepository.FindByIdAsync(userId, cancellationToken);
        if (account is null || account.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<UserProfile>.Unauthorized("Invalid user.");
        }

        StoredMedia uploadResult;
        try
        {
            uploadResult = await _avatarStorage.UploadAsync(command.Content! with { OwnerId = userId }, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return AuthOperationResult<UserProfile>.ValidationFailure(exception.Message);
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var profile = await _accountRepository.UpdateAvatarAsync(userId, uploadResult.Url, DateTime.UtcNow, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return AuthOperationResult<UserProfile>.Success(profile!);
    }

    public async Task<AuthOperationResult<UserProfile>> UpdateLearningProfileAsync(
        uint userId,
        UpdateLearningProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var invalidReference = await GetInvalidLearningProfileReferenceAsync(command, cancellationToken);
        if (invalidReference is not null)
        {
            return AuthOperationResult<UserProfile>.ValidationFailure($"{invalidReference} is invalid.");
        }

        var account = await _accountRepository.FindByIdAsync(userId, cancellationToken);
        if (account is null || account.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<UserProfile>.Unauthorized("Invalid user.");
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var profile = await _accountRepository.UpsertLearningProfileAsync(userId, command, DateTime.UtcNow, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await RemoveCachedProfileAsync(userId, cancellationToken);
        await RemoveCachedKnnTopicRecommendationsAsync(userId, cancellationToken);

        return AuthOperationResult<UserProfile>.Success(profile!);
    }

    public async Task<AuthOperationResult<OtpSendResult>> SendOtpAsync(
        OtpSendCommand command,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var purpose = NormalizeOtpPurpose(command.Purpose);
        if (!OtpPurpose.All.Contains(purpose))
        {
            return AuthOperationResult<OtpSendResult>.ValidationFailure("Purpose must be register, verify, or reset.");
        }

        var now = DateTime.UtcNow;
        var recentOtp = await _otpRepository.FindLatestAsync(
            phone,
            purpose,
            userId: null,
            since: now.AddSeconds(-_rateLimitOptions.RetryAfterSeconds),
            cancellationToken);
        if (recentOtp is not null)
        {
            return AuthOperationResult<OtpSendResult>.TooManyRequests("OTP request rate limit exceeded.");
        }

        var user = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);
        uint? otpUserId = null;
        if (purpose == OtpPurpose.Reset)
        {
            if (user?.PasswordHash is null || user.Status == UserStatus.Deleted)
            {
                return AuthOperationResult<OtpSendResult>.NotFound("User not found.");
            }

            otpUserId = user.UserId;
        }
        else if (user is not null && user.Status != UserStatus.Deleted)
        {
            return AuthOperationResult<OtpSendResult>.Conflict("Phone already exists.");
        }

        var otpCode = _otpCodeGenerator.Generate();
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _otpRepository.StageCreateAsync(
            new CreateOtpRecord(
                otpUserId,
                phone,
                otpCode,
                IsUsed: false,
                OtpStatus.Active,
                VerifyAttemptCount: 0,
                now.AddMinutes(AppSettings.OtpTtlMinutes),
                now),
            cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _smsSender.SendOtpAsync(phone, otpCode, cancellationToken);

        return AuthOperationResult<OtpSendResult>.Success(new OtpSendResult(AppSettings.OtpTtlMinutes * 60));
    }

    public async Task<AuthOperationResult<OtpVerificationResult>> VerifyOtpAsync(
        OtpVerifyCommand command,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var result = await ValidateOtpAsync(phone, userId: null, command.OtpCode!, persistInvalidAttempt: true, cancellationToken);
        if (!result.IsSuccess)
        {
            return PropagateFailure<OtpVerificationResult, OtpRecord>(result);
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _otpRepository.StageUsedAsync(result.Value!, userId: null, DateTime.UtcNow, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<OtpVerificationResult>.Success(new OtpVerificationResult(true));
    }

    public async Task<AuthOperationResult<OtpSendResult>> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var user = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<OtpSendResult>.NotFound("User not found.");
        }

        return await SendOtpAsync(new OtpSendCommand(phone, OtpPurpose.Reset), cancellationToken);
    }

    public async Task<AuthOperationResult<OtpVerificationResult>> VerifyResetOtpAsync(
        OtpVerifyCommand command,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var user = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<OtpVerificationResult>.Unauthorized("Invalid phone or OTP.");
        }

        var result = await ValidateOtpAsync(phone, user.UserId, command.OtpCode!, persistInvalidAttempt: true, cancellationToken);
        if (!result.IsSuccess)
        {
            return PropagateFailure<OtpVerificationResult, OtpRecord>(result);
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _otpRepository.StageAttemptAsync(result.Value!.OtpId, result.Value.VerifyAttemptCount - 1, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthOperationResult<OtpVerificationResult>.Success(new OtpVerificationResult(true));
    }

    public async Task<AuthOperationResult<bool>> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var phone = command.Phone!.Trim();
        var user = await _accountRepository.FindByPhoneAsync(phone, cancellationToken);
        if (user?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid phone or OTP.");
        }

        var otpResult = await ValidateOtpAsync(phone, user.UserId, command.OtpCode!, persistInvalidAttempt: true, cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return PropagateFailure<bool, OtpRecord>(otpResult);
        }

        if (_passwordHasher.Verify(command.NewPassword!, user.PasswordHash))
        {
            return AuthOperationResult<bool>.Conflict("New password must be different from current password.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        var lockedOtp = await _otpRepository.FindLatestForUpdateAsync(
            phone,
            userId: user.UserId,
            cancellationToken: cancellationToken);
        if (lockedOtp is null || lockedOtp.OtpId != otpResult.Value!.OtpId)
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid phone or OTP.");
        }

        await _accountRepository.UpdatePasswordAsync(user.UserId, _passwordHasher.Hash(command.NewPassword!), now, cancellationToken);
        await _otpRepository.StageUsedAsync(otpResult.Value!, user.UserId, now, cancellationToken);
        await _refreshTokenRepository.StageRevokeAllAsync(user.UserId, now, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<bool>.Success(true);
    }

    public async Task<AuthOperationResult<bool>> ChangePasswordAsync(
        uint userId,
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _accountRepository.FindByIdAsync(userId, cancellationToken);
        if (user?.PasswordHash is null || user.Status == UserStatus.Deleted)
        {
            return AuthOperationResult<bool>.Unauthorized("Invalid user.");
        }

        if (!_passwordHasher.Verify(command.CurrentPassword!, user.PasswordHash))
        {
            return AuthOperationResult<bool>.Unauthorized("Current password is incorrect.");
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _accountRepository.UpdatePasswordAsync(user.UserId, _passwordHasher.Hash(command.NewPassword!), DateTime.UtcNow, cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthOperationResult<bool>.Success(true);
    }

    private async Task<AuthOperationResult<OtpRecord>> ValidateOtpAsync(
        string phone,
        uint? userId,
        string otpCode,
        bool persistInvalidAttempt,
        CancellationToken cancellationToken)
    {
        var otp = await _otpRepository.FindLatestAsync(phone, userId: userId, cancellationToken: cancellationToken);
        if (otp is null)
        {
            return AuthOperationResult<OtpRecord>.Unauthorized("Invalid OTP.");
        }

        var now = DateTime.UtcNow;
        if (otp.ExpiresAt <= now)
        {
            return AuthOperationResult<OtpRecord>.Unauthorized("OTP has expired.");
        }

        if (otp.IsUsed)
        {
            return AuthOperationResult<OtpRecord>.Conflict("OTP has already been used.");
        }

        if (otp.VerifyAttemptCount >= AppSettings.OtpMaxVerifyAttempts)
        {
            return AuthOperationResult<OtpRecord>.TooManyRequests("Maximum OTP verify attempts exceeded.");
        }

        var updated = otp with { VerifyAttemptCount = otp.VerifyAttemptCount + 1 };
        if (otp.OtpCode != otpCode)
        {
            if (persistInvalidAttempt)
            {
                await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
                await _otpRepository.StageAttemptAsync(otp.OtpId, updated.VerifyAttemptCount, cancellationToken);
                await transaction.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return AuthOperationResult<OtpRecord>.Unauthorized("Invalid OTP.");
        }

        return AuthOperationResult<OtpRecord>.Success(updated);
    }

    private async Task<AuthOperationResult<AuthTokenPair>> SignInAccountAsync(
        AuthAccount user,
        SignInContext signInContext,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        await _accountRepository.StageLastLoginAsync(user.UserId, now, cancellationToken);
        await _refreshTokenRepository.StageCreateAsync(CreateRefreshToken(user.UserId, refreshToken, now, signInContext), cancellationToken);
        await transaction.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthOperationResult<AuthTokenPair>.Success(CreateTokenPair(user.UserId, user.RoleName, refreshToken));
    }

    private CreateRefreshToken CreateRefreshToken(
        uint userId,
        string refreshToken,
        DateTime now,
        SignInContext signInContext) =>
        new(
            userId,
            _refreshTokenHasher.Hash(refreshToken),
            signInContext.DeviceInfo,
            signInContext.IpAddress,
            now.AddDays(_tokenOptions.RefreshTokenDays),
            now);

    private AuthTokenPair CreateTokenPair(uint userId, string role, string refreshToken) =>
        new(
            _jwtTokenService.GenerateAccessToken(userId, role),
            refreshToken,
            _tokenOptions.AccessTokenMinutes * 60);

    private static string NormalizeOtpPurpose(string? purpose) =>
        string.IsNullOrWhiteSpace(purpose) ? OtpPurpose.Verify : purpose.Trim().ToLowerInvariant();

    private static AuthOperationResult<TOut> PropagateFailure<TOut, TIn>(AuthOperationResult<TIn> result)
    {
        return result.ErrorKind switch
        {
            AuthErrorKind.Unauthorized => AuthOperationResult<TOut>.Unauthorized(result.Error!),
            AuthErrorKind.Forbidden => AuthOperationResult<TOut>.Forbidden(result.Error!),
            AuthErrorKind.NotFound => AuthOperationResult<TOut>.NotFound(result.Error!),
            AuthErrorKind.Conflict => AuthOperationResult<TOut>.Conflict(result.Error!),
            AuthErrorKind.TooManyRequests => AuthOperationResult<TOut>.TooManyRequests(result.Error!),
            _ => AuthOperationResult<TOut>.ValidationFailure(result.Error!),
        };
    }

    private static AuthOperationResult<T>? ValidateSignInStatus<T>(
        AuthAccount user,
        string deletedMessage,
        string lockedMessage)
    {
        if (user.Status == UserStatus.Locked)
        {
            return AuthOperationResult<T>.Forbidden(lockedMessage);
        }

        return user.Status == UserStatus.Deleted
            ? AuthOperationResult<T>.Unauthorized(deletedMessage)
            : null;
    }

    private static string ResolveGoogleDisplayName(GoogleIdentity googleUser) =>
        !string.IsNullOrWhiteSpace(googleUser.Name) ? googleUser.Name : googleUser.Email ?? "Google User";

    private async Task<string?> GetInvalidRegistrationProfileReferenceAsync(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RegionId.HasValue
            && !await _accountRepository.ActiveRegionExistsAsync(command.RegionId.Value, cancellationToken))
        {
            return nameof(RegisterCommand.RegionId);
        }

        if (command.OccupationId.HasValue
            && !await _accountRepository.ActiveOccupationExistsAsync(command.OccupationId.Value, cancellationToken))
        {
            return nameof(RegisterCommand.OccupationId);
        }

        if (command.EducationLevelId.HasValue
            && !await _accountRepository.ActiveEducationLevelExistsAsync(command.EducationLevelId.Value, cancellationToken))
        {
            return nameof(RegisterCommand.EducationLevelId);
        }

        return null;
    }

    private async Task<LearningProfile?> BuildRegistrationLearningProfileAsync(
        RegisterCommand command,
        DateTime now,
        CancellationToken cancellationToken)
    {
        uint? ageRangeId = null;
        if (command.DateOfBirth.HasValue)
        {
            var age = AgeHelper.CalculateAge(command.DateOfBirth.Value, DateOnly.FromDateTime(now));
            ageRangeId = await _accountRepository.ResolveAgeRangeIdByAgeAsync(age, cancellationToken);
        }

        if (ageRangeId is null
            && command.RegionId is null
            && command.OccupationId is null
            && command.EducationLevelId is null)
        {
            return null;
        }

        return new LearningProfile(
            ageRangeId,
            command.RegionId,
            command.OccupationId,
            command.EducationLevelId,
            LearningPurposeId: null);
    }

    private async Task<string?> GetInvalidLearningProfileReferenceAsync(
        UpdateLearningProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (command.AgeRangeId.HasValue
            && !await _accountRepository.ActiveAgeRangeExistsAsync(command.AgeRangeId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileCommand.AgeRangeId);
        }

        if (command.RegionId.HasValue
            && !await _accountRepository.ActiveRegionExistsAsync(command.RegionId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileCommand.RegionId);
        }

        if (command.OccupationId.HasValue
            && !await _accountRepository.ActiveOccupationExistsAsync(command.OccupationId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileCommand.OccupationId);
        }

        if (command.EducationLevelId.HasValue
            && !await _accountRepository.ActiveEducationLevelExistsAsync(command.EducationLevelId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileCommand.EducationLevelId);
        }

        if (command.LearningPurposeId.HasValue
            && !await _accountRepository.ActiveLearningPurposeExistsAsync(command.LearningPurposeId.Value, cancellationToken))
        {
            return nameof(UpdateLearningProfileCommand.LearningPurposeId);
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

    private async Task RemoveCachedKnnTopicRecommendationsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_knnTopicRecommendationCache is not null)
        {
            await _knnTopicRecommendationCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static string? ValidateAvatarFile(UploadedContent? content)
    {
        if (content is null || content.Length == 0)
        {
            return "Avatar file is required.";
        }

        if (content.Length > MaxAvatarFileBytes)
        {
            return "Avatar file must be 5MB or smaller.";
        }

        if (!AllowedAvatarContentTypes.Contains(content.ContentType))
        {
            return "Avatar MIME type must be one of: image/jpeg, image/png, image/webp.";
        }

        return null;
    }

    private static readonly IReadOnlySet<string> AllowedAvatarContentTypes = new HashSet<string>(
        ["image/jpeg", "image/png", "image/webp"],
        StringComparer.OrdinalIgnoreCase);

    private sealed class NullOtpCodeGenerator : IOtpCodeGenerator
    {
        public static readonly NullOtpCodeGenerator Instance = new();

        private NullOtpCodeGenerator()
        {
        }

        public string Generate() => "000000";
    }

    private sealed class NullSmsSender : ISmsSender
    {
        public static readonly NullSmsSender Instance = new();

        private NullSmsSender()
        {
        }

        public Task SendOtpAsync(string phone, string code, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
