using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;
using AuthUserProfile = VocaNova.API.Features.Auth.BLL.Models.UserProfile;

namespace VocaNova.API.Features.Auth.DAL.Mappings;

internal static class AuthPersistenceMappings
{
    public static AuthRole ToAuthRole(this Role role) => new(role.RoleId, role.RoleName);

    public static AuthAccount ToAuthAccount(this User user) =>
        new(
            user.UserId,
            user.RoleId,
            user.Role.RoleName,
            user.Status,
            user.LastLoginAt,
            user.UserAuth?.Phone,
            user.UserAuth?.PasswordHash,
            user.UserAuth?.IsPhoneVerified ?? false,
            user.UserAuth?.GoogleUid,
            user.UserAuth?.GoogleEmail,
            user.UserAuth?.Username,
            user.UserProfile?.FullName,
            user.UserProfile?.AvatarUrl,
            user.UserLearningProfile?.ToLearningProfile());

    public static AuthUserProfile ToUserProfile(this User user) =>
        new(
            user.UserId,
            user.UserAuth?.Phone,
            user.UserProfile?.FullName ?? string.Empty,
            user.UserProfile?.AvatarUrl,
            user.Role.RoleName,
            user.Status,
            user.UserLearningProfile?.ToLearningProfile());

    public static LearningProfile ToLearningProfile(this UserLearningProfile profile) =>
        new(
            profile.AgeRangeId,
            profile.RegionId,
            profile.OccupationId,
            profile.EducationLevelId,
            profile.LearningPurposeId);

    public static RefreshTokenRecord ToRefreshTokenRecord(this RefreshToken token) =>
        new(
            token.TokenId,
            token.UserId,
            token.TokenHash,
            token.ExpiresAt,
            token.RevokedAt,
            token.User.Status,
            token.User.Role.RoleName);

    public static OtpRecord ToOtpRecord(this OtpVerification otp) =>
        new(
            otp.OtpId,
            otp.UserId,
            otp.Phone,
            otp.OtpCode,
            otp.IsUsed,
            otp.Status,
            otp.VerifyAttemptCount,
            otp.ExpiresAt,
            otp.CreatedAt);
}
