using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed record UpdateListWordNoteRequest(
    [property: JsonPropertyName("note")] string? Note);
