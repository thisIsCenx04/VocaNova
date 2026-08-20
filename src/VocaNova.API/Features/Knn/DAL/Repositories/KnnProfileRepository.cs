using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Knn.DAL.Repositories;

public sealed class KnnProfileRepository : IKnnProfileRepository
{
    private static readonly string[] InterestSources = TopicPreferenceSource.NeighborSources.ToArray();

    private readonly VocaNovaDbContext _dbContext;

    public KnnProfileRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KnnLearningProfile?> GetLearningProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new KnnLearningProfile(
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

    public async Task<IReadOnlyCollection<KnnTopicPreference>> GetActiveTopicPreferencesAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId
                && preference.Status == UserStatus.Active)
            .OrderBy(preference => preference.Topic.TopicName)
            .ThenBy(preference => preference.TopicId)
            .Select(preference => new KnnTopicPreference(
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

    public async Task<KnnProfileVectorSource?> GetProfileVectorSourceAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var interestTopicIds = await GetInterestTopicIdsQuery()
            .Where(preference => preference.UserId == userId)
            .Select(preference => preference.TopicId)
            .ToListAsync(cancellationToken);

        var profile = await _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new KnnProfileVectorSource(
                profile.UserId,
                profile.AgeRangeId,
                profile.RegionId,
                profile.OccupationId,
                profile.EducationLevelId,
                profile.LearningPurposeId,
                null))
            .SingleOrDefaultAsync(cancellationToken);

        // A user who skipped the sign-up questions has no learning-profile row yet, but may
        // still have picked topics during onboarding. Those picks alone are enough for a vector.
        if (profile is null)
        {
            return interestTopicIds.Count == 0
                ? null
                : new KnnProfileVectorSource(userId, null, null, null, null, null, interestTopicIds);
        }

        return profile with { InterestTopicIds = interestTopicIds };
    }

    public async Task<KnnLookupDimensions> GetActiveLookupDimensionsAsync(
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
        var topicIds = await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(topic => topic.TopicId)
            .Select(topic => topic.TopicId)
            .ToListAsync(cancellationToken);

        return new KnnLookupDimensions(
            ageRangeIds,
            regionIds,
            occupationIds,
            educationLevelIds,
            learningPurposeIds,
            topicIds);
    }

    public async Task<LearningProfileOptions> GetActiveLookupOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var ageRanges = await _dbContext.AgeRanges
            .AsNoTracking()
            .Where(ageRange => ageRange.Status == UserStatus.Active)
            .OrderBy(ageRange => ageRange.DisplayOrder)
            .ThenBy(ageRange => ageRange.AgeRangeId)
            .Select(ageRange => new LearningProfileOption(ageRange.AgeRangeId, ageRange.Name))
            .ToListAsync(cancellationToken);
        var regions = await _dbContext.Regions
            .AsNoTracking()
            .Where(region => region.Status == UserStatus.Active)
            .OrderBy(region => region.Name)
            .ThenBy(region => region.RegionId)
            .Select(region => new LearningProfileOption(region.RegionId, region.Name))
            .ToListAsync(cancellationToken);
        var occupations = await _dbContext.Occupations
            .AsNoTracking()
            .Where(occupation => occupation.Status == UserStatus.Active)
            .OrderBy(occupation => occupation.Name)
            .ThenBy(occupation => occupation.OccupationId)
            .Select(occupation => new LearningProfileOption(occupation.OccupationId, occupation.Name))
            .ToListAsync(cancellationToken);
        var educationLevels = await _dbContext.EducationLevels
            .AsNoTracking()
            .Where(educationLevel => educationLevel.Status == UserStatus.Active)
            .OrderBy(educationLevel => educationLevel.DisplayOrder)
            .ThenBy(educationLevel => educationLevel.EducationLevelId)
            .Select(educationLevel => new LearningProfileOption(
                educationLevel.EducationLevelId,
                educationLevel.Name))
            .ToListAsync(cancellationToken);
        var learningPurposes = await _dbContext.LearningPurposes
            .AsNoTracking()
            .Where(learningPurpose => learningPurpose.Status == UserStatus.Active)
            .OrderBy(learningPurpose => learningPurpose.Name)
            .ThenBy(learningPurpose => learningPurpose.LearningPurposeId)
            .Select(learningPurpose => new LearningProfileOption(
                learningPurpose.LearningPurposeId,
                learningPurpose.Name))
            .ToListAsync(cancellationToken);

        return new LearningProfileOptions(
            ageRanges,
            regions,
            occupations,
            educationLevels,
            learningPurposes);
    }

    public async Task<IReadOnlyCollection<KnnProfileVectorSource>> GetCandidateProfileSourcesAsync(
        uint excludingUserId,
        CancellationToken cancellationToken = default)
    {
        // Two flat queries joined in memory rather than a per-candidate lookup, so the candidate
        // scan stays at a constant number of round trips regardless of how many users exist.
        var profiles = await _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId != excludingUserId
                && profile.User.Status == UserStatus.Active
                && (profile.AgeRangeId.HasValue
                    || profile.RegionId.HasValue
                    || profile.OccupationId.HasValue
                    || profile.EducationLevelId.HasValue
                    || profile.LearningPurposeId.HasValue))
            .Select(profile => new KnnProfileVectorSource(
                profile.UserId,
                profile.AgeRangeId,
                profile.RegionId,
                profile.OccupationId,
                profile.EducationLevelId,
                profile.LearningPurposeId,
                null))
            .ToListAsync(cancellationToken);

        var interestRows = await GetInterestTopicIdsQuery()
            .Where(preference => preference.UserId != excludingUserId
                && preference.User.Status == UserStatus.Active)
            .Select(preference => new { preference.UserId, preference.TopicId })
            .ToListAsync(cancellationToken);
        var topicIdsByUserId = interestRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<uint>)group.Select(row => row.TopicId).ToArray());

        var candidates = profiles
            .Select(profile => topicIdsByUserId.TryGetValue(profile.UserId, out var topicIds)
                ? profile with { InterestTopicIds = topicIds }
                : profile)
            .ToList();

        // Users whose only signal is their onboarding topic picks are still valid neighbours.
        var profiledUserIds = profiles.Select(profile => profile.UserId).ToHashSet();
        candidates.AddRange(topicIdsByUserId
            .Where(entry => !profiledUserIds.Contains(entry.Key))
            .Select(entry => new KnnProfileVectorSource(
                entry.Key,
                null,
                null,
                null,
                null,
                null,
                entry.Value)));

        return candidates;
    }

    /// <summary>
    /// Topic picks that express what the learner wants to study (onboarding answers and manual
    /// selections), as opposed to topics the system itself suggested.
    /// </summary>
    private IQueryable<UserTopicPreference> GetInterestTopicIdsQuery()
    {
        return _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => preference.Status == UserStatus.Active
                && InterestSources.Contains(preference.Source));
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

    public async Task<IReadOnlyCollection<KnnTopicPreference>> GetNeighborTopicPreferencesAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlySet<string> sources,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0 || sources.Count == 0)
        {
            return Array.Empty<KnnTopicPreference>();
        }

        var sourceValues = sources.ToArray();
        return await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(preference => userIds.Contains(preference.UserId)
                && preference.Status == UserStatus.Active
                && sourceValues.Contains(preference.Source))
            .Select(preference => new KnnTopicPreference(
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

    public async Task<IReadOnlyCollection<NeighborPersonalTopic>> GetNeighborPersonalTopicsAsync(
        uint currentUserId,
        IReadOnlyCollection<uint> neighborUserIds,
        int wordsPerTopic,
        CancellationToken cancellationToken = default)
    {
        if (neighborUserIds.Count == 0)
        {
            return Array.Empty<NeighborPersonalTopic>();
        }

        var lists = await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => neighborUserIds.Contains(list.UserId)
                && list.Status == UserStatus.Active
                && list.ListName.StartsWith(PersonalTopicListName.Prefix)
                && list.UserListWords.Any())
            .Select(list => new { list.ListId, list.UserId, list.ListName })
            .ToListAsync(cancellationToken);

        var parsedLists = lists
            .Select(list => PersonalTopicListName.TryGetTopicId(list.ListName, out var topicId)
                ? new { list.ListId, list.UserId, TopicId = topicId }
                : null)
            .Where(list => list is not null)
            .Select(list => list!)
            .ToArray();
        if (parsedLists.Length == 0)
        {
            return Array.Empty<NeighborPersonalTopic>();
        }

        var topicIds = parsedLists.Select(list => list.TopicId).Distinct().ToArray();
        var topics = await _dbContext.Topics
            .AsNoTracking()
            .Where(topic => topicIds.Contains(topic.TopicId))
            .Select(topic => new
            {
                topic.TopicId,
                topic.TopicName,
                topic.TopicNameVi,
                topic.Icon,
            })
            .ToDictionaryAsync(topic => topic.TopicId, cancellationToken);

        var currentUserWordIds = await _dbContext.UserListWords
            .AsNoTracking()
            .Where(item => item.UserId == currentUserId)
            .Select(item => item.WordId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var listIds = parsedLists.Select(list => list.ListId).ToArray();
        var wordRows = await _dbContext.UserListWords
            .AsNoTracking()
            .Where(item => listIds.Contains(item.ListId)
                && !currentUserWordIds.Contains(item.WordId))
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new
            {
                item.ListId,
                item.WordId,
                Word = item.Word.Word1,
                Phonetic = item.Word.PhoneticUk,
                Cefr = item.Word.CefrLevel,
                PrimaryMeaning = item.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .Select(sense => sense.VietnameseMeaning ?? sense.EnglishDefinition)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var wordsByListId = wordRows
            .GroupBy(row => row.ListId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PersonalTopicRecommendationWord>)group
                    .Take(wordsPerTopic)
                    .Select(row => new PersonalTopicRecommendationWord(
                        row.WordId,
                        row.Word,
                        row.Phonetic,
                        row.Cefr,
                        row.PrimaryMeaning))
                    .ToArray());

        return parsedLists
            .Where(list => topics.ContainsKey(list.TopicId)
                && wordsByListId.TryGetValue(list.ListId, out var words)
                && words.Count > 0)
            .Select(list =>
            {
                var topic = topics[list.TopicId];
                var words = wordsByListId[list.ListId];
                return new NeighborPersonalTopic(
                    list.UserId,
                    list.TopicId,
                    topic.TopicName,
                    topic.TopicNameVi,
                    topic.Icon,
                    wordRows.Count(row => row.ListId == list.ListId),
                    words);
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TopicRecommendation>> GetFallbackTopicRecommendationsAsync(
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
            .Select(row => new TopicRecommendation(
                row.TopicId,
                row.TopicName,
                row.TopicNameVi,
                row.Icon,
                row.WordCount,
                row.Score))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TopicRecommendation>> GetTopicRecommendationsByScoreAsync(
        IReadOnlyDictionary<uint, double> scoresByTopicId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (scoresByTopicId.Count == 0)
        {
            return Array.Empty<TopicRecommendation>();
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
            .Select(row => new TopicRecommendation(
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

    public async Task<int?> ReplaceOnboardingTopicPreferencesAsync(
        uint userId,
        IReadOnlyCollection<uint> topicIds,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var requestedTopicIds = topicIds.Distinct().ToArray();
        if (requestedTopicIds.Length > 0)
        {
            var existingTopicCount = await _dbContext.Topics
                .CountAsync(topic => requestedTopicIds.Contains(topic.TopicId), cancellationToken);
            if (existingTopicCount != requestedTopicIds.Length)
            {
                return null;
            }
        }

        var currentPreferences = await _dbContext.UserTopicPreferences
            .Where(preference => preference.UserId == userId)
            .ToListAsync(cancellationToken);
        var currentByTopicId = currentPreferences.ToDictionary(preference => preference.TopicId);

        foreach (var topicId in requestedTopicIds)
        {
            if (currentByTopicId.TryGetValue(topicId, out var preference))
            {
                preference.Source = TopicPreferenceSource.Onboarding;
                preference.Status = UserStatus.Active;
            }
            else
            {
                _dbContext.UserTopicPreferences.Add(new UserTopicPreference
                {
                    UserId = userId,
                    TopicId = topicId,
                    Source = TopicPreferenceSource.Onboarding,
                    Status = UserStatus.Active,
                    CreatedAt = now,
                });
            }
        }

        // Deselecting during onboarding must not wipe topics the user accepted from a KNN
        // suggestion or added by hand later, so only previous onboarding rows are retired.
        var requestedTopicIdSet = requestedTopicIds.ToHashSet();
        foreach (var preference in currentPreferences)
        {
            if (preference.Source == TopicPreferenceSource.Onboarding
                && !requestedTopicIdSet.Contains(preference.TopicId))
            {
                preference.Status = UserStatus.Deleted;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return requestedTopicIds.Length;
    }
}
