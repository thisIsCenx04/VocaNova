namespace VocaNova.API.Infrastructure.Caching;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string Configuration { get; set; } = "localhost:6379";

    public string InstanceName { get; set; } = "vocanova:";
}
