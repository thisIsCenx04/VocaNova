using FluentAssertions;

namespace VocaNova.Tests.Architecture;

public sealed class FeatureFirstLayoutArchitectureTests
{
    [Fact]
    public void Migrated_Slices_Should_Not_Leave_Source_In_Superseded_Top_Level_Layers()
    {
        var apiRoot = Path.Combine(FindRepositoryRoot(), "src", "VocaNova.API");
        foreach (var layer in new[] { "Controllers", "Contracts", "Mappings", "BLL", "DAL" })
        {
            var path = Path.Combine(apiRoot, layer);
            var files = Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                : [];

            files.Should().BeEmpty($"ADR-018 supersedes the top-level {layer} source root");
        }
    }

    [Fact]
    public void Migrated_Slices_Should_Use_Corrected_Feature_First_Boundaries()
    {
        var apiRoot = Path.Combine(FindRepositoryRoot(), "src", "VocaNova.API");
        foreach (var feature in new[]
                 {
                     "Notifications", "Progress", "Dictionary", "Lists", "Quiz", "AiGrading",
                 })
        {
            var featureRoot = Path.Combine(apiRoot, "Features", feature);
            Directory.Exists(Path.Combine(featureRoot, "Controllers")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "Contracts", "Requests")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "Contracts", "Responses")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "Mappings")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "BLL", "Abstractions")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "BLL", "Models")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "BLL", "Services")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "DAL", "Repositories")).Should().BeTrue();
            Directory.Exists(Path.Combine(featureRoot, "DAL", "Mappings")).Should().BeTrue();
            Directory.EnumerateFiles(
                    Path.Combine(featureRoot, "DAL", "Repositories"),
                    "I*Repository.cs",
                    SearchOption.TopDirectoryOnly)
                .Should().BeEmpty("repository interfaces are owned by the feature BLL");
        }
    }

    [Theory]
    [InlineData("Notifications")]
    [InlineData("Progress")]
    [InlineData("Dictionary")]
    [InlineData("Lists")]
    [InlineData("Quiz")]
    [InlineData("AiGrading")]
    public void Migrated_Feature_Bll_Should_Remain_Framework_And_Outer_Layer_Independent(
        string feature)
    {
        var bllRoot = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "VocaNova.API",
            "Features",
            feature,
            "BLL");
        var forbidden = new[]
        {
            $"VocaNova.API.Features.{feature}.Controllers",
            $"VocaNova.API.Features.{feature}.Contracts",
            $"VocaNova.API.Features.{feature}.DAL",
            "VocaNova.API.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "StackExchange.Redis",
            "VocaNovaDbContext",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var reference in forbidden)
            {
                source.Should().NotContain(
                    reference,
                    $"{feature} BLL source {Path.GetFileName(file)} must remain boundary-independent");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VocaNova.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
