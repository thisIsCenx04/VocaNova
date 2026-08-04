namespace VocaNova.API.Features.Knn.DTOs;

public sealed record WordRecommendationItem(
    uint WordId,
    string Word,
    string? PhoneticUk,
    string? PrimaryMeaning,
    string? ImageUrl,
    string? CefrLevel,
    double Score);
