using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

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

    public Task<KnnProfileVectorSourceDto?> GetProfileVectorSourceAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new KnnProfileVectorSourceDto(
                profile.UserId,
                profile.AgeRangeId,
                profile.RegionId,
                profile.OccupationId,
                profile.EducationLevelId,
                profile.LearningPurposeId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<KnnLookupDimensionsDto> GetActiveLookupDimensionsAsync(
        CancellationToken cancellationToken = default)
    {
        var ageRangeIds = await _dbContext.AgeRanges
            .AsNoTracking()
            .Where(ageRange => ageRange.Status == UserStatus.Active)
            .OrderBy(ageRange => ageRange.AgeRangeId)
            .Select(ageRange => ageRange.AgeRangeId)
            .ToListAsync(cancellationToken);
        var regionIds = await _dbContext.Regions
            .AsNoTracking()
            .Where(region => region.Status == UserStatus.Active)
            .OrderBy(region => region.RegionId)
            .Select(region => region.RegionId)
            .ToListAsync(cancellationToken);
        var occupationIds = await _dbContext.Occupations
            .AsNoTracking()
            .Where(occupation => occupation.Status == UserStatus.Active)
            .OrderBy(occupation => occupation.OccupationId)
            .Select(occupation => occupation.OccupationId)
            .ToListAsync(cancellationToken);
        var educationLevelIds = await _dbContext.EducationLevels
            .AsNoTracking()
            .Where(educationLevel => educationLevel.Status == UserStatus.Active)
            .OrderBy(educationLevel => educationLevel.EducationLevelId)
            .Select(educationLevel => educationLevel.EducationLevelId)
            .ToListAsync(cancellationToken);
        var learningPurposeIds = await _dbContext.LearningPurposes
            .AsNoTracking()
            .Where(learningPurpose => learningPurpose.Status == UserStatus.Active)
            .OrderBy(learningPurpose => learningPurpose.LearningPurposeId)
            .Select(learningPurpose => learningPurpose.LearningPurposeId)
            .ToListAsync(cancellationToken);

        return new KnnLookupDimensionsDto(
            ageRangeIds,
            regionIds,
            occupationIds,
            educationLevelIds,
            learningPurposeIds);
    }

    public async Task<IReadOnlyCollection<KnnProfileVectorSourceDto>> GetCandidateProfileSourcesAsync(
        uint excludingUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId != excludingUserId
                && profile.User.Status == UserStatus.Active
                && (profile.AgeRangeId.HasValue
                    || profile.RegionId.HasValue
                    || profile.OccupationId.HasValue
                    || profile.EducationLevelId.HasValue
                    || profile.LearningPurposeId.HasValue))
            .Select(profile => new KnnProfileVectorSourceDto(
                profile.UserId,
                profile.AgeRangeId,
                profile.RegionId,
                profile.OccupationId,
                profile.EducationLevelId,
                profile.LearningPurposeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<uint>> GetActiveTopicIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId
                && preference.Status == UserStatus.Active)
            .Select(preference => preference.TopicId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnnTopicPreferenceDto>> GetNeighborTopicPreferencesAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlySet<string> sources,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0 || sources.Count == 0)
        {
            return Array.Empty<KnnTopicPreferenceDto>();
        }

        var sourceValues = sources.ToArray();
        return await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => userIds.Contains(preference.UserId)
                && preference.Status == UserStatus.Active
                && sourceValues.Contains(preference.Source))
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

    public async Task<IReadOnlyCollection<TopicRecommendationDto>> GetFallbackTopicRecommendationsAsync(
        IReadOnlyCollection<uint> excludedTopicIds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => preference.Status == UserStatus.Active
                && !excludedTopicIds.Contains(preference.TopicId))
            .GroupBy(preference => new
            {
                preference.TopicId,
                preference.Topic.TopicName,
                preference.Topic.TopicNameVi,
                preference.Topic.Icon,
            })
            .Select(group => new
            {
                group.Key.TopicId,
                group.Key.TopicName,
                group.Key.TopicNameVi,
                group.Key.Icon,
                Score = (double)group.Count(),
                WordCount = _dbContext.WordTopics.Count(wordTopic => wordTopic.TopicId == group.Key.TopicId),
            })
            .OrderByDescending(row => row.Score)
            .ThenBy(row => row.TopicName)
            .ThenBy(row => row.TopicId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new TopicRecommendationDto(
                row.TopicId,
                row.TopicName,
                row.TopicNameVi,
                row.Icon,
                row.WordCount,
                row.Score))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TopicRecommendationDto>> GetTopicRecommendationsByScoreAsync(
        IReadOnlyDictionary<uint, double> scoresByTopicId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (scoresByTopicId.Count == 0)
        {
            return Array.Empty<TopicRecommendationDto>();
        }

        var topicIds = scoresByTopicId.Keys.ToArray();
        var rows = await _dbContext.Topics
            .AsNoTracking()
            .Where(topic => topicIds.Contains(topic.TopicId))
            .Select(topic => new
            {
                topic.TopicId,
                topic.TopicName,
                topic.TopicNameVi,
                topic.Icon,
                WordCount = topic.WordTopics.Count,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new TopicRecommendationDto(
                row.TopicId,
                row.TopicName,
                row.TopicNameVi,
                row.Icon,
                row.WordCount,
                scoresByTopicId[row.TopicId]))
            .OrderByDescending(topic => topic.RecommendationScore)
            .ThenBy(topic => topic.TopicName)
            .ThenBy(topic => topic.TopicId)
            .Take(limit)
            .ToArray();
    }

    public async Task<bool> UpsertTopicPreferenceAsync(
        uint userId,
        uint topicId,
        string source,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var topicExists = await _dbContext.Topics
            .AnyAsync(topic => topic.TopicId == topicId, cancellationToken);
        if (!topicExists)
        {
            return false;
        }

        var preference = await _dbContext.UserTopicPreferences
            .SingleOrDefaultAsync(
                entity => entity.UserId == userId && entity.TopicId == topicId,
                cancellationToken);
        if (preference is null)
        {
            _dbContext.UserTopicPreferences.Add(new UserTopicPreference
            {
                UserId = userId,
                TopicId = topicId,
                Source = source,
                Status = UserStatus.Active,
                CreatedAt = now,
            });
        }
        else
        {
            preference.Source = source;
            preference.Status = UserStatus.Active;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
