using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed record UpdateListWordNoteRequest(
    [property: JsonPropertyName("note")] string? Note);
