namespace VocaNova.API.Common.Constants;

public static class AudioStatus
{
    public const string Pending = "pending";
    public const string Uploaded = "uploaded";
    public const string TtsGenerated = "tts_generated";
    public const string Missing = "missing";
    public const string Deleted = "deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Uploaded,
        TtsGenerated,
        Missing,
        Deleted,
    };

    public static readonly IReadOnlySet<string> Playable = new HashSet<string>(StringComparer.Ordinal)
    {
        Uploaded,
        TtsGenerated,
    };
}
