namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; set; }

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string Folder { get; set; } = "vocanova/words";

    public string AvatarFolder { get; set; } = "vocanova/avatars";
}
