namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class UploadWordImageRequest
{
    public IFormFile? File { get; set; }
}
