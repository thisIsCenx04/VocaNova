namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class MediaSuggestionRequest
{
    public string? Query { get; set; }

    public string? Type { get; set; } = "image";

    public int Limit { get; set; } = 8;
}
