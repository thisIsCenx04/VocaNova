namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class UploadAvatarRequest
{
    public IFormFile? File { get; set; }
}
