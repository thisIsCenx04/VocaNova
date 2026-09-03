using System.Text.Json.Serialization;
using FluentAssertions;

namespace VocaNova.Tests.AiGrading;

public sealed class AiGradingControllerContractTests
{
    [Theory]
    [MemberData(nameof(JsonContractCases))]
    public void Contracts_Should_Preserve_Explicit_Json_Names(Type type, string[] expectedNames)
    {
        type.GetProperties()
            .Select(property => property
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .Single().Name)
            .Should().Equal(expectedNames);
    }

    public static TheoryData<Type, string[]> JsonContractCases => new()
    {
        { typeof(UpdateAiGradingConfigRequest), new[] { "provider", "endpoint", "model", "fallback_models", "api_key", "max_attempts", "retry_base_delay_ms", "attempt_timeout_seconds", "pass_threshold" } },
        { typeof(AiGradingConfigResponse), new[] { "provider", "endpoint", "model", "fallback_models", "max_attempts", "retry_base_delay_ms", "attempt_timeout_seconds", "pass_threshold", "has_api_key", "api_key_hint", "storage", "can_write_env_file", "supported_providers" } },
        { typeof(AiGradingConnectionTestResponse), new[] { "succeeded", "model", "elapsed_ms", "message" } },
    };
}
