using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed record CreateListRequest(
    [property: JsonPropertyName("list_name")] string? ListName);
