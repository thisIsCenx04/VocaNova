using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Services;

/// <summary>
/// Builds the hybrid profile vector used to find similar learners.
///
/// The vector concatenates six blocks:
///   [age | region | occupation | education]  — captured by the sign-up form
///   [learning purpose | interest topics]     — captured by the onboarding questions
///
/// Each block is L2-normalised before being scaled by its configured weight. Normalising per
/// block is what keeps the wide multi-hot topic block from swamping the narrow one-hot blocks,
/// and it makes a user who picked ten topics comparable to one who picked two. Blocks the user
/// left blank simply contribute zeros, which shrinks the norm without distorting the direction —
/// that is why every field can stay optional.
/// </summary>
public static class KnnProfileVectorBuilder
{
    public static double[] Build(
        KnnProfileVectorSourceDto profile,
        KnnLookupDimensionsDto dimensions,
        KnnVectorOptions weights)
    {
        var values = new List<double>(
            dimensions.AgeRangeIds.Count
            + dimensions.RegionIds.Count
            + dimensions.OccupationIds.Count
            + dimensions.EducationLevelIds.Count
            + dimensions.LearningPurposeIds.Count
            + dimensions.TopicIds.Count);

        AppendOneHot(values, dimensions.AgeRangeIds, profile.AgeRangeId, weights.AgeRangeWeight);
        AppendOneHot(values, dimensions.RegionIds, profile.RegionId, weights.RegionWeight);
        AppendOneHot(values, dimensions.OccupationIds, profile.OccupationId, weights.OccupationWeight);
        AppendOneHot(
            values,
            dimensions.EducationLevelIds,
            profile.EducationLevelId,
            weights.EducationLevelWeight);
        AppendOneHot(
            values,
            dimensions.LearningPurposeIds,
            profile.LearningPurposeId,
            weights.LearningPurposeWeight);
        AppendNormalizedMultiHot(
            values,
            dimensions.TopicIds,
            profile.InterestTopicIds,
            weights.InterestTopicsWeight);

        return values.ToArray();
    }

    public static bool IsZeroVector(double[] vector)
    {
        return vector.All(value => value == 0.0);
    }

    /// <summary>
    /// A one-hot block is already unit length when a value is selected, so weighting is the
    /// only scaling needed.
    /// </summary>
    private static void AppendOneHot(
        List<double> values,
        IReadOnlyList<uint> activeIds,
        uint? selectedId,
        double weight)
    {
        foreach (var activeId in activeIds)
        {
            values.Add(selectedId == activeId ? weight : 0.0);
        }
    }

    /// <summary>
    /// A multi-hot block with <c>n</c> selections has length sqrt(n), so each set entry is
    /// divided by sqrt(n) to bring the block back to unit length before weighting.
    /// </summary>
    private static void AppendNormalizedMultiHot(
        List<double> values,
        IReadOnlyList<uint> activeIds,
        IReadOnlyCollection<uint>? selectedIds,
        double weight)
    {
        if (selectedIds is null || selectedIds.Count == 0)
        {
            for (var index = 0; index < activeIds.Count; index++)
            {
                values.Add(0.0);
            }

            return;
        }

        var selectedIdSet = selectedIds as IReadOnlySet<uint> ?? selectedIds.ToHashSet();
        var matchedCount = activeIds.Count(selectedIdSet.Contains);
        if (matchedCount == 0)
        {
            for (var index = 0; index < activeIds.Count; index++)
            {
                values.Add(0.0);
            }

            return;
        }

        var scale = weight / Math.Sqrt(matchedCount);
        foreach (var activeId in activeIds)
        {
            values.Add(selectedIdSet.Contains(activeId) ? scale : 0.0);
        }
    }
}
