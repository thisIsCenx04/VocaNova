namespace VocaNova.API.Features.Auth.DTOs;

public sealed class UploadAvatarRequest
{
    public IFormFile? File { get; set; }
}
