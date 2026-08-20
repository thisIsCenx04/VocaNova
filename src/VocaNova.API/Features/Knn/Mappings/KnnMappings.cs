using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.Contracts.Responses;

namespace VocaNova.API.Features.Knn.Mappings;

public static class KnnMappings
{
    public static LearningProfileOptionsResponse ToResponse(this LearningProfileOptions value) =>
        new(
            value.AgeRanges.Select(ToResponse).ToArray(),
            value.Regions.Select(ToResponse).ToArray(),
            value.Occupations.Select(ToResponse).ToArray(),
            value.EducationLevels.Select(ToResponse).ToArray(),
            value.LearningPurposes.Select(ToResponse).ToArray());

    public static IReadOnlyCollection<TopicRecommendationResponse> ToResponse(
        this IReadOnlyCollection<TopicRecommendation> values) =>
        values.Select(item => new TopicRecommendationResponse(
            item.TopicId,
            item.TopicName,
            item.TopicNameVi,
            item.Icon,
            item.WordCount,
            item.RecommendationScore)).ToArray();

    public static IReadOnlyCollection<WordRecommendationResponse> ToResponse(
        this IReadOnlyCollection<WordRecommendation> values) =>
        values.Select(item => new WordRecommendationResponse(
            item.WordId,
            item.Word,
            item.PhoneticUk,
            item.PrimaryMeaning,
            item.ImageUrl,
            item.CefrLevel,
            item.Score)).ToArray();

    public static IReadOnlyCollection<PersonalTopicRecommendationResponse> ToResponse(
        this IReadOnlyCollection<PersonalTopicRecommendation> values) =>
        values.Select(item => new PersonalTopicRecommendationResponse(
            item.TopicId,
            item.Name,
            item.NameVi,
            item.Icon,
            item.WordCount,
            item.RecommendationScore,
            item.Words.Select(word => new PersonalTopicRecommendationWordResponse(
                word.WordId,
                word.Word,
                word.Phonetic,
                word.Cefr,
                word.PrimaryMeaning)).ToArray())).ToArray();

    private static LearningProfileOptionResponse ToResponse(LearningProfileOption item) =>
        new(item.Id, item.Name);
}
