namespace VocaNova.API.Features.Knn.DTOs;

/// <summary>
/// A word a neighbour has in their study lists, used by the cold-start recommendation path.
/// </summary>
public sealed record KnnNeighborWordDto(uint UserId, uint WordId);
