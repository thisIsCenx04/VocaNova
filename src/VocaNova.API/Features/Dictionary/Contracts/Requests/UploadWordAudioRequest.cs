namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class UploadWordAudioRequest
{
    public string? Accent { get; set; }
    public IFormFile? File { get; set; }
}
