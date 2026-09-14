namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class UploadWordVideoRequest
{
    public IFormFile? File { get; set; }
}
