using FluentAssertions;
using Moq;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Infrastructure.Storage;
using AudioModel = VocaNova.API.Features.Dictionary.BLL.Models.WordAudio;
using UploadedContent = VocaNova.API.Features.Dictionary.BLL.Models.UploadedContent;
using StoredMedia = VocaNova.API.Features.Dictionary.BLL.Models.StoredMedia;

namespace VocaNova.Tests.Dictionary;

public sealed class WordAudioLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacement_Deletes_Previous_File_Only_If_Unreferenced(bool referenced)
    {
        var (repository, storage, service) = CreateService();
        repository.Setup(r => r.UpsertAudioAsync(1, It.IsAny<StoredMedia>(), "uk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioReplacement(new AudioModel(5, "uk", "uploaded", "new", "uploaded"), "old"));
        repository.Setup(r => r.IsAudioUrlReferencedAsync("old", It.IsAny<CancellationToken>())).ReturnsAsync(referenced);

        var result = await service.UploadAudioAsync(1, "uk", Content());

        result.IsSuccess.Should().BeTrue();
        storage.Verify(s => s.DeleteOwnedAsync("old", It.IsAny<CancellationToken>()), referenced ? Times.Never() : Times.Once());
        storage.Verify(s => s.DeleteOwnedAsync("new", It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_Save_Deletes_New_File_Only_After_Checking_Commit_State(bool committed)
    {
        var (repository, storage, service) = CreateService();
        repository.Setup(r => r.UpsertAudioAsync(1, It.IsAny<StoredMedia>(), "uk", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));
        repository.Setup(r => r.IsAudioUrlReferencedAsync("new", It.IsAny<CancellationToken>())).ReturnsAsync(committed);

        var action = () => service.UploadAudioAsync(1, "uk", Content());
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");

        storage.Verify(s => s.DeleteOwnedAsync("new", It.IsAny<CancellationToken>()), committed ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task Cleanup_Failure_Does_Not_Convert_Saved_Audio_To_Failure()
    {
        var (repository, storage, service) = CreateService();
        repository.Setup(r => r.UpsertAudioAsync(1, It.IsAny<StoredMedia>(), "uk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioReplacement(new AudioModel(5, "uk", "uploaded", "new", "uploaded"), "old"));
        storage.Setup(s => s.DeleteOwnedAsync("old", It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        (await service.UploadAudioAsync(1, "uk", Content())).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/demo/video/upload/v123/vocanova/words/audio/1/uk/run.mp3", "vocanova/words/audio/1/uk/run")]
    [InlineData("https://res.cloudinary.com/other/video/upload/v123/vocanova/words/audio/1/uk/run.mp3", null)]
    [InlineData("https://example.com/demo/video/upload/v123/vocanova/words/audio/1/uk/run.mp3", null)]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v123/vocanova/words/audio/1/uk/run.mp3", null)]
    [InlineData("https://res.cloudinary.com/demo/video/upload/v123/avatars/1/uk/run.mp3", null)]
    [InlineData("https://res.cloudinary.com/demo/video/upload/v123/vocanova/words/audio/1/uk/sub/run.mp3", null)]
    public void Cleanup_Only_Resolves_Owned_Audio_Urls(string url, string? expected) =>
        CloudinaryWordAudioStorage.GetOwnedPublicId(url, "demo", "vocanova/words/audio").Should().Be(expected);

    private static UploadedContent Content() => new("run.mp3", "audio/mpeg", 4, new MemoryStream([1, 2, 3, 4]));

    private static (Mock<IWordAdminRepository>, Mock<IWordAudioStorage>, WordAdminService) CreateService()
    {
        var repository = new Mock<IWordAdminRepository>();
        var storage = new Mock<IWordAudioStorage>();
        repository.Setup(r => r.WordExistsAsync(1, false, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storage.Setup(s => s.UploadAsync(It.IsAny<UploadedContent>(), "uk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredMedia("new-key", "new"));
        return (repository, storage, new WordAdminService(repository.Object, audioStorage: storage.Object));
    }
}
