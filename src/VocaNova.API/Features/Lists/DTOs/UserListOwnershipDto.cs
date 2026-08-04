namespace VocaNova.API.Features.Lists.DTOs;

public sealed record UserListOwnershipDto(
    uint ListId,
    uint UserId,
    string Status,
    string ListName);
