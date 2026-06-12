namespace VocaNova.API.Features.Lists.DTOs;

public sealed record ListWordStateDto(
    uint UserId,
    uint ListId,
    uint WordId,
    string Status);
