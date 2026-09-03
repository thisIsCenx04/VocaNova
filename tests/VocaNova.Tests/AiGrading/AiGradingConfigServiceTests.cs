using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Admin.Contracts.Requests;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;
using VocaNova.API.Features.AiGrading.Contracts.Requests;
using VocaNova.API.Features.AiGrading.Contracts.Responses;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;
using VocaNova.Tests.Support;

namespace VocaNova.Tests.AiGrading;

public class AiGradingConfigServiceTests
{
    [Fact]
    public async Task GetConfigAsync_Should_Read_From_Configuration()
    {
        var service = CreateService(out _);

        var config = await GetConfigViewAsync(service);

        config.Storage.Should().Be("env_file");
        config.Model.Should().Be("configured-model");
        config.HasApiKey.Should().BeTrue();
        config.SupportedProviders.Should().Contain("Gemini");
    }

    [Fact]
    public async Task GetConfigAsync_Should_Never_Return_The_Raw_Api_Key()
    {
        var service = CreateService(out _);

        var config = await GetConfigViewAsync(service);

        config.ApiKeyHint.Should().NotBe("configured-secret-key");
        config.ApiKeyHint.Should().EndWith("-key");
        config.ApiKeyHint.Should().StartWith("••••");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Keep_Existing_Key_When_Left_Blank()
    {
        var service = CreateService(out var writer);

        await UpdateConfigViewAsync(service, EmptyRequest with { Model = "new-model", ApiKey = "   " });

        writer.WrittenValues["AiGrading__Model"].Should().Be("new-model");
        writer.WrittenValues["AiGrading__ApiKey"].Should().Be("configured-secret-key");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Replace_The_Key_When_Provided()
    {
        var service = CreateService(out var writer);

        await UpdateConfigViewAsync(service, EmptyRequest with { ApiKey = "rotated-key" });

        writer.WrittenValues["AiGrading__ApiKey"].Should().Be("rotated-key");
        writer.WrittenValues["AiGrading__Model"].Should().Be("configured-model");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Clamp_Out_Of_Range_Values()
    {
        var service = CreateService(out var writer);

        var config = await UpdateConfigViewAsync(service, EmptyRequest with
        {
            MaxAttempts = 99,
            AttemptTimeoutSeconds = 600,
            RetryBaseDelayMs = 99_999,
            PassThreshold = 5,
        });

        config.MaxAttempts.Should().Be(4);
        writer.WrittenValues["AiGrading__MaxAttempts"].Should().Be("4");
        writer.WrittenValues["AiGrading__AttemptTimeoutSeconds"].Should().Be("15");
        writer.WrittenValues["AiGrading__RetryBaseDelayMs"].Should().Be("5000");
        // Invariant formatting, so a machine using ',' as the decimal separator still writes a
        // value the configuration binder can read back.
        writer.WrittenValues["AiGrading__PassThreshold"].Should().Be("1");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Normalize_Endpoint_And_Fallback_Models()
    {
        var service = CreateService(out var writer);

        var config = await UpdateConfigViewAsync(service, EmptyRequest with
        {
            Endpoint = " https://example.test/v1/ ",
            FallbackModels = [" a ", "A", "", "b"],
        });

        config.Endpoint.Should().Be("https://example.test/v1");
        config.FallbackModels.Should().Equal("a", "b");
        writer.WrittenValues["AiGrading__FallbackModels__0"].Should().Be("a");
        writer.WrittenValues["AiGrading__FallbackModels__1"].Should().Be("b");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Delete_Stale_Fallback_Model_Slots()
    {
        var service = CreateService(out var writer);
        await UpdateConfigViewAsync(service, EmptyRequest with { FallbackModels = ["a", "b", "c"] });

        await UpdateConfigViewAsync(service, EmptyRequest with { FallbackModels = ["a"] });

        writer.WrittenValues["AiGrading__FallbackModels__0"].Should().Be("a");
        // Shrinking the list must remove the trailing entries, otherwise .env would keep
        // feeding the dropped models back in.
        writer.WrittenValues.Should().NotContainKey("AiGrading__FallbackModels__1");
        writer.WrittenValues.Should().NotContainKey("AiGrading__FallbackModels__2");
    }

    [Fact]
    public async Task ResetConfigAsync_Should_Restore_Defaults_But_Keep_The_Api_Key()
    {
        var service = CreateService(out var writer);
        await UpdateConfigViewAsync(service, EmptyRequest with { Model = "temporary-model" });

        var config = await ResetConfigViewAsync(service);

        var defaults = new AiGradingSettings();
        config.Model.Should().Be(defaults.Model);
        config.PassThreshold.Should().Be(defaults.PassThreshold);
        // Wiping the credential would take grading offline, which is not what resetting the
        // tuning is meant to do.
        config.HasApiKey.Should().BeTrue();
        writer.WrittenValues["AiGrading__ApiKey"].Should().Be("configured-secret-key");
    }

    [Fact]
    public async Task UpdateConfigAsync_Should_Use_The_Fallback_Store_When_Env_File_Is_Not_Writable()
    {
        var service = CreateService(out var writer, out _, canWriteEnvFile: false);

        var config = await UpdateConfigViewAsync(service, EmptyRequest with { Model = "fallback-model" });

        config.Storage.Should().Be("fallback");
        config.CanWriteEnvFile.Should().BeFalse();
        writer.WrittenValues.Should().BeEmpty();
        // No file to watch, so the fallback store is what grading reads — immediately.
        (await service.GetEffectiveSettingsAsync()).Model.Should().Be("fallback-model");
        (await GetConfigViewAsync(service)).Storage.Should().Be("fallback");
    }

    [Fact]
    public async Task GetEffectiveSettingsAsync_Should_Follow_Configuration_Reloads()
    {
        var service = CreateService(out _, out var monitor, canWriteEnvFile: true);

        await UpdateConfigViewAsync(service, EmptyRequest with { Model = "written-model" });

        // The write landed in .env; the watcher has not fired yet.
        (await service.GetEffectiveSettingsAsync()).Model.Should().Be("configured-model");

        monitor.Set(new AiGradingSettings { Model = "written-model", ApiKey = "configured-secret-key" });

        (await service.GetEffectiveSettingsAsync()).Model.Should().Be("written-model");
    }

    [Fact]
    public void IsSupportedProvider_Should_Reject_Providers_Without_A_Client()
    {
        AiGradingConfigService.IsSupportedProvider("Gemini").Should().BeTrue();
        AiGradingConfigService.IsSupportedProvider("gemini").Should().BeTrue();
        AiGradingConfigService.IsSupportedProvider("OpenAI").Should().BeFalse();
        AiGradingConfigService.IsSupportedProvider(null).Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Reject_Out_Of_Range_And_Malformed_Values()
    {
        var validator = new UpdateAiGradingConfigRequestValidator();

        validator.TestValidate(EmptyRequest with { Endpoint = "not-a-url" })
            .ShouldHaveValidationErrorFor(request => request.Endpoint);
        validator.TestValidate(EmptyRequest with { MaxAttempts = 0 })
            .ShouldHaveValidationErrorFor(request => request.MaxAttempts!.Value);
        validator.TestValidate(EmptyRequest with { AttemptTimeoutSeconds = 60 })
            .ShouldHaveValidationErrorFor(request => request.AttemptTimeoutSeconds!.Value);
        validator.TestValidate(EmptyRequest with { PassThreshold = 1.5 })
            .ShouldHaveValidationErrorFor(request => request.PassThreshold!.Value);
        validator.TestValidate(EmptyRequest with { FallbackModels = ["a", "b", "c", "d", "e", "f"] })
            .ShouldHaveValidationErrorFor(request => request.FallbackModels!);

        // An untouched form posts nothing but the values it did change.
        validator.TestValidate(EmptyRequest).IsValid.Should().BeTrue();
    }

    private static readonly UpdateAiGradingConfigRequest EmptyRequest = new(
        Provider: null,
        Endpoint: null,
        Model: null,
        FallbackModels: null,
        ApiKey: null,
        MaxAttempts: null,
        RetryBaseDelayMs: null,
        AttemptTimeoutSeconds: null,
        PassThreshold: null);

    private static async Task<AiGradingConfigurationView> GetConfigViewAsync(
        AiGradingConfigService service)
    {
        var result = await service.GetConfigAsync();
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static async Task<AiGradingConfigurationView> UpdateConfigViewAsync(
        AiGradingConfigService service,
        UpdateAiGradingConfigRequest request)
    {
        var result = await service.UpdateConfigAsync(ToCommand(request));
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static async Task<AiGradingConfigurationView> ResetConfigViewAsync(
        AiGradingConfigService service)
    {
        var result = await service.ResetConfigAsync();
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static UpdateAiGradingConfigurationCommand ToCommand(UpdateAiGradingConfigRequest request) =>
        new(
            request.Provider,
            request.Endpoint,
            request.Model,
            request.FallbackModels,
            request.ApiKey,
            request.MaxAttempts,
            request.RetryBaseDelayMs,
            request.AttemptTimeoutSeconds,
            request.PassThreshold);

    private static AiGradingConfigService CreateService(out FakeRuntimeConfigWriter writer)
    {
        return CreateService(out writer, out _, canWriteEnvFile: true);
    }

    /// <summary>
    /// The fake writer only records what would go into .env; configuration is not reloaded
    /// automatically, so tests drive <paramref name="monitor"/> when they need to simulate the
    /// file watcher firing.
    /// </summary>
    private static AiGradingConfigService CreateService(
        out FakeRuntimeConfigWriter writer,
        out MutableOptionsMonitor<AiGradingSettings> monitor,
        bool canWriteEnvFile)
    {
        var store = new InMemoryRuntimeSettingsStore();
        writer = new FakeRuntimeConfigWriter(store, canWriteEnvFile);
        monitor = new MutableOptionsMonitor<AiGradingSettings>(new AiGradingSettings
        {
            Provider = "Gemini",
            Endpoint = "https://generativelanguage.googleapis.com/v1beta",
            Model = "configured-model",
            FallbackModels = ["configured-fallback"],
            ApiKey = "configured-secret-key",
            MaxAttempts = 2,
            RetryBaseDelayMs = 400,
            AttemptTimeoutSeconds = 6,
            PassThreshold = 0.75,
        });

        return new AiGradingConfigService(store, writer, monitor);
    }
}
