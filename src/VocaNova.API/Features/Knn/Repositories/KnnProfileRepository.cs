using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Knn.Repositories;

public sealed class KnnProfileRepository : IKnnProfileRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public KnnProfileRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KnnLearningProfileDto?> GetLearningProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new KnnLearningProfileDto(
                profile.UserId,
                profile.AgeRangeId,
                profile.AgeRange == null ? null : profile.AgeRange.Name,
                profile.RegionId,
                profile.Region == null ? null : profile.Region.Name,
                profile.OccupationId,
                profile.Occupation == null ? null : profile.Occupation.Name,
                profile.EducationLevelId,
                profile.EducationLevel == null ? null : profile.EducationLevel.Name,
                profile.LearningPurposeId,
                profile.LearningPurpose == null ? null : profile.LearningPurpose.Name,
                profile.CreatedAt,
                profile.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnnTopicPreferenceDto>> GetActiveTopicPreferencesAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId
                && preference.Status == UserStatus.Active)
            .OrderBy(preference => preference.Topic.TopicName)
            .ThenBy(preference => preference.TopicId)
            .Select(preference => new KnnTopicPreferenceDto(
                preference.UserId,
                preference.TopicId,
                preference.Topic.TopicName,
                preference.Topic.TopicNameVi,
                preference.Topic.Icon,
                preference.Source,
                preference.Status,
                preference.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
