namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class ImportWordsRequest
{
    public IFormFile? File { get; set; }
}
