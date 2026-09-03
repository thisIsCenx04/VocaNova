namespace VocaNova.API.Features.Auth.BLL.Models;

public sealed record AuthRole(uint RoleId, string RoleName);

public sealed record AuthAccount(
    uint UserId,
    uint RoleId,
    string RoleName,
    string Status,
    DateTime? LastLoginAt,
    string? Phone,
    string? PasswordHash,
    bool IsPhoneVerified,
    string? GoogleSubject,
    string? GoogleEmail,
    string? UserName,
    string? FullName,
    string? AvatarUrl,
    LearningProfile? LearningProfile);

public sealed record CreateAuthAccount(
    uint RoleId,
    string Status,
    string? Phone,
    string? PasswordHash,
    bool IsPhoneVerified,
    string? GoogleSubject,
    string? GoogleEmail,
    string? FullName,
    string? AvatarUrl,
    LearningProfile? LearningProfile,
    DateTime CreatedAt);

public sealed record RefreshTokenRecord(
    uint RefreshTokenId,
    uint UserId,
    string TokenHash,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string UserStatus,
    string RoleName);

public sealed record CreateRefreshToken(
    uint UserId,
    string TokenHash,
    string? DeviceInfo,
    string? IpAddress,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public sealed record OtpRecord(
    uint OtpId,
    uint? UserId,
    string Phone,
    string OtpCode,
    bool IsUsed,
    string Status,
    int VerifyAttemptCount,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public sealed record CreateOtpRecord(
    uint? UserId,
    string Phone,
    string OtpCode,
    bool IsUsed,
    string Status,
    int VerifyAttemptCount,
    DateTime ExpiresAt,
    DateTime CreatedAt);
