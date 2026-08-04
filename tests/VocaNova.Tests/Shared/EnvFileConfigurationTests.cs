using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.Tests.Shared;

public class EnvFileConfigurationTests
{
    [Theory]
    [InlineData("KEY=value", "KEY", "value")]
    [InlineData("  KEY = value  ", "KEY", "value")]
    [InlineData("export KEY=value", "KEY", "value")]
    [InlineData("KEY=\"quoted value\"", "KEY", "quoted value")]
    [InlineData("KEY='quoted value'", "KEY", "quoted value")]
    [InlineData("KEY=", "KEY", "")]
    [InlineData("KEY=a=b", "KEY", "a=b")]
    public void TryParseLine_Should_Read_Supported_Forms(string line, string key, string value)
    {
        EnvFileConfigurationProvider.TryParseLine(line, out var parsedKey, out var parsedValue)
            .Should().BeTrue();
        parsedKey.Should().Be(key);
        parsedValue.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# a comment")]
    [InlineData("  # indented comment")]
    [InlineData("NOT_AN_ASSIGNMENT")]
    [InlineData("=missing-key")]
    public void TryParseLine_Should_Skip_Blanks_Comments_And_Malformed_Lines(string line)
    {
        EnvFileConfigurationProvider.TryParseLine(line, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Load_Should_Map_DoubleUnderscore_To_Configuration_Sections()
    {
        var provider = new EnvFileConfigurationProvider(new EnvFileConfigurationSource());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            """
            # comment
            AiGrading__Model=gemini-test
            AiGrading__FallbackModels__0=first
            Knn__Vector__AgeRangeWeight=2.5
            """));

        provider.Load(stream);

        provider.TryGet("AiGrading:Model", out var model).Should().BeTrue();
        model.Should().Be("gemini-test");
        provider.TryGet("AiGrading:FallbackModels:0", out var fallback).Should().BeTrue();
        fallback.Should().Be("first");
        provider.TryGet("Knn:Vector:AgeRangeWeight", out var weight).Should().BeTrue();
        weight.Should().Be("2.5");
    }

    [Fact]
    public void ApplyValues_Should_Rewrite_In_Place_And_Preserve_Comments_And_Order()
    {
        var lines = new List<string>
        {
            "# Database",
            "MYSQL_CONNECTION_STRING=Server=localhost;",
            string.Empty,
            "AiGrading__Model=old-model",
            "AiGrading__ApiKey=old-key",
            "Redis__Configuration=localhost:6379",
        };

        var result = EnvFileRuntimeConfigWriter.ApplyValues(lines, new Dictionary<string, string?>
        {
            ["AiGrading__Model"] = "new-model",
            ["AiGrading__ApiKey"] = "new-key",
        });

        result.Should().Equal(
            "# Database",
            "MYSQL_CONNECTION_STRING=Server=localhost;",
            string.Empty,
            "AiGrading__Model=new-model",
            "AiGrading__ApiKey=new-key",
            "Redis__Configuration=localhost:6379");
    }

    [Fact]
    public void ApplyValues_Should_Append_Unknown_Keys_Under_A_Marked_Section()
    {
        var lines = new List<string> { "EXISTING=1" };

        var result = EnvFileRuntimeConfigWriter.ApplyValues(lines, new Dictionary<string, string?>
        {
            ["Knn__Vector__AgeRangeWeight"] = "1.5",
        });

        result[0].Should().Be("EXISTING=1");
        result.Should().Contain(line => line.StartsWith("# ---", StringComparison.Ordinal));
        result.Should().Contain("Knn__Vector__AgeRangeWeight=1.5");
    }

    [Fact]
    public void ApplyValues_Should_Delete_Lines_For_Null_Values()
    {
        var lines = new List<string>
        {
            "AiGrading__FallbackModels__0=a",
            "AiGrading__FallbackModels__1=b",
            "KEEP=1",
        };

        var result = EnvFileRuntimeConfigWriter.ApplyValues(lines, new Dictionary<string, string?>
        {
            ["AiGrading__FallbackModels__0"] = "a",
            ["AiGrading__FallbackModels__1"] = null,
        });

        result.Should().Equal("AiGrading__FallbackModels__0=a", "KEEP=1");
    }

    [Fact]
    public void ApplyValues_Should_Quote_Values_That_Would_Otherwise_Be_Misread()
    {
        var result = EnvFileRuntimeConfigWriter.ApplyValues([], new Dictionary<string, string?>
        {
            ["PLAIN"] = "simple",
            ["HASH"] = "abc#def",
            ["QUOTED"] = "say \"hi\"",
        });

        result.Should().Contain("PLAIN=simple");
        // Without quoting, everything from '#' onwards would be re-read as a comment.
        result.Should().Contain("HASH=\"abc#def\"");
        result.Should().Contain("QUOTED=\"say \\\"hi\\\"\"");
    }

    [Fact]
    public void ApplyValues_Round_Trips_Through_The_Provider()
    {
        var lines = EnvFileRuntimeConfigWriter.ApplyValues([], new Dictionary<string, string?>
        {
            ["AiGrading__ApiKey"] = "key with spaces#and-hash",
            ["Knn__Vector__InterestTopicsWeight"] = "2",
        });

        var provider = new EnvFileConfigurationProvider(new EnvFileConfigurationSource());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        provider.Load(stream);

        provider.TryGet("AiGrading:ApiKey", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("key with spaces#and-hash");
        provider.TryGet("Knn:Vector:InterestTopicsWeight", out var weight).Should().BeTrue();
        weight.Should().Be("2");
    }

    [Fact]
    public void Written_Values_Should_Bind_To_The_Settings_Objects()
    {
        var lines = EnvFileRuntimeConfigWriter.ApplyValues(
            [],
            VocaNova.API.Features.Knn.Services.KnnRuntimeConfigService.ToEnvValues(
                new VocaNova.API.Features.Knn.DTOs.KnnVectorWeightsDto(1.5, 0.5, 1, 0.25, 2, 3)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(ToConfigurationPairs(lines))
            .Build();
        var options = configuration.GetSection("Knn").Get<VocaNova.API.Features.Knn.KnnOptions>();

        // Proves the env key names line up with the options graph; a rename on either side
        // would otherwise silently fall back to defaults.
        options!.Vector.AgeRangeWeight.Should().Be(1.5);
        options.Vector.EducationLevelWeight.Should().Be(0.25);
        options.Vector.InterestTopicsWeight.Should().Be(3);
    }

    private static IEnumerable<KeyValuePair<string, string?>> ToConfigurationPairs(
        IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (EnvFileConfigurationProvider.TryParseLine(line, out var key, out var value))
            {
                yield return new KeyValuePair<string, string?>(
                    key.Replace("__", ":", StringComparison.Ordinal),
                    value);
            }
        }
    }
}
