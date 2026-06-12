using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        VocaNovaDbContext dbContext,
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IGoogleTokenVerifier googleTokenVerifier,
        IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _googleTokenVerifier = googleTokenVerifier;
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
}
