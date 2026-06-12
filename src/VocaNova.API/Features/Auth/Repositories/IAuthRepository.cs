using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.Repositories;

public interface IAuthRepository
{
    Task<User?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<User?> FindByGoogleUidAsync(string googleUid, CancellationToken cancellationToken = default);

    Task<User?> FindByGoogleEmailAsync(string googleEmail, CancellationToken cancellationToken = default);

    Task<User?> FindByIdAsync(uint userId, CancellationToken cancellationToken = default);

    Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<User> CreateUserAsync(
        User user,
        UserAuth userAuth,
        UserProfile userProfile,
        UserLearningProfile? learningProfile = null,
        CancellationToken cancellationToken = default);

    Task<RefreshToken> CreateRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        RefreshToken refreshToken,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task UpdateUserProfileAsync(
        User user,
        string displayName,
        string? avatarUrl,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<UserLearningProfile> UpsertLearningProfileAsync(
        User user,
        uint? ageRangeId,
        uint? regionId,
        uint? occupationId,
        uint? educationLevelId,
        uint? learningPurposeId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveAgeRangeExistsAsync(uint ageRangeId, CancellationToken cancellationToken = default);

    Task<bool> ActiveRegionExistsAsync(uint regionId, CancellationToken cancellationToken = default);

    Task<bool> ActiveOccupationExistsAsync(uint occupationId, CancellationToken cancellationToken = default);

    Task<bool> ActiveEducationLevelExistsAsync(uint educationLevelId, CancellationToken cancellationToken = default);

    Task<bool> ActiveLearningPurposeExistsAsync(uint learningPurposeId, CancellationToken cancellationToken = default);

    Task<OtpVerification?> FindLatestOtpByPhoneAsync(
        string phone,
        CancellationToken cancellationToken = default);

    Task<OtpVerification?> FindLatestOtpByPhoneSinceAsync(
        string phone,
        DateTime createdSince,
        CancellationToken cancellationToken = default);

    Task<OtpVerification> CreateOtpAsync(
        OtpVerification otpVerification,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> RevokeTokenAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);
}
