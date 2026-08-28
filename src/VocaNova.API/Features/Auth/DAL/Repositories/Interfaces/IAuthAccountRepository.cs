using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IAuthAccountRepository
{
    Task<AuthAccount?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<AuthAccount?> FindByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default);

    Task<AuthAccount?> FindByGoogleEmailAsync(string googleEmail, CancellationToken cancellationToken = default);

    Task<AuthAccount?> FindByIdAsync(uint userId, CancellationToken cancellationToken = default);

    Task<AuthRole?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task StageCreateAsync(CreateAuthAccount account, CancellationToken cancellationToken = default);

    Task StageLastLoginAsync(uint userId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<UserProfile?> GetProfileAsync(uint userId, CancellationToken cancellationToken = default);

    Task<UserProfile?> UpdateProfileAsync(uint userId, UpdateProfileCommand command, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<UserProfile?> UpdateAvatarAsync(uint userId, string avatarUrl, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<UserProfile?> UpsertLearningProfileAsync(uint userId, UpdateLearningProfileCommand command, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<bool> ActiveAgeRangeExistsAsync(uint ageRangeId, CancellationToken cancellationToken = default);

    Task<uint?> ResolveAgeRangeIdByAgeAsync(int age, CancellationToken cancellationToken = default);

    Task<bool> ActiveRegionExistsAsync(uint regionId, CancellationToken cancellationToken = default);

    Task<bool> ActiveOccupationExistsAsync(uint occupationId, CancellationToken cancellationToken = default);

    Task<bool> ActiveEducationLevelExistsAsync(uint educationLevelId, CancellationToken cancellationToken = default);

    Task<bool> ActiveLearningPurposeExistsAsync(uint learningPurposeId, CancellationToken cancellationToken = default);

    Task UpdatePasswordAsync(uint userId, string passwordHash, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<bool> StageSoftDeleteAsync(uint userId, DateTime now, CancellationToken cancellationToken = default);
}
