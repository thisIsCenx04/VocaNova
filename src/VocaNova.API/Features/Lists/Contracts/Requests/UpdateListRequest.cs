using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed record UpdateListRequest(
    [property: JsonPropertyName("list_name")] string? ListName);
