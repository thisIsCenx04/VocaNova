namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed class UploadWordAudioRequest
{
    public string? Accent { get; set; }

    public IFormFile? File { get; set; }
}
