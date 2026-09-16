using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services;
using UploadedContent = VocaNova.API.Features.Dictionary.BLL.Models.UploadedContent;

namespace VocaNova.Tests.Dictionary;

public sealed class WordVideoLifecycleTests
{
    private readonly Mock<IWordVideoRepository> repository = new();
    private readonly Mock<IWordVideoStorage> storage = new();
    private readonly Mock<IWordDetailCache> cache = new();
    private readonly WordVideoService service;
    private static readonly StoredWordVideo Prepared = new("new", "https://example.test/video.mp4", "https://example.test/thumb.jpg");

    public WordVideoLifecycleTests()
    {
        repository.Setup(r => r.WordExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storage.Setup(s => s.UploadAsync(It.IsAny<UploadedContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadedWordVideo("new", 6, 1920, 1080, "mp4", true));
        storage.Setup(s => s.PrepareAsync(It.IsAny<UploadedWordVideo>(), It.IsAny<CancellationToken>())).ReturnsAsync(Prepared);
        repository.Setup(r => r.ReplaceAsync(1, Prepared, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoReplacement(new WordVideo(1, "uploaded", Prepared.Url, Prepared.ThumbnailUrl, "active"), "old"));
        service = new WordVideoService(repository.Object, storage.Object, cache.Object, NullLogger<WordVideoService>.Instance);
    }

    [Theory]
    [InlineData(0, "swim.mp4", "video/mp4")]
    [InlineData(20971521, "swim.mp4", "video/mp4")]
    [InlineData(10, "swim.mov", "video/mp4")]
    [InlineData(10, "swim.mp4", "audio/mp4")]
    public async Task Invalid_File_Is_Rejected_Before_Upload(long length, string filename, string mime)
    {
        (await service.SaveAsync(1, new UploadedContent(filename, mime, length, Stream.Null))).IsSuccess.Should().BeFalse();
        storage.Verify(s => s.UploadAsync(It.IsAny<UploadedContent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(4.99, true, false)]
    [InlineData(5, true, true)]
    [InlineData(15, true, true)]
    [InlineData(20, true, true)]
    [InlineData(20.49, true, true)]
    [InlineData(20.51, true, false)]
    [InlineData(double.NaN, true, false)]
    [InlineData(6, false, false)]
    public async Task Trusted_Metadata_Controls_Acceptance(double duration, bool hasVideo, bool accepted)
    {
        storage.Setup(s => s.UploadAsync(It.IsAny<UploadedContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadedWordVideo("new", duration, 1920, 1080, "mp4", hasVideo));
        (await service.SaveAsync(1, Content())).IsSuccess.Should().Be(accepted);
        repository.Verify(r => r.ReplaceAsync(1, Prepared, It.IsAny<CancellationToken>()), accepted ? Times.Once() : Times.Never());
        storage.Verify(s => s.DeleteAsync("new", It.IsAny<CancellationToken>()), accepted ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_Save_Checks_Commit_State_Before_Cleanup(bool committed)
    {
        repository.Setup(r => r.ReplaceAsync(1, Prepared, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Connection lost"));
        repository.Setup(r => r.IsReferencedAsync("new", It.IsAny<CancellationToken>())).ReturnsAsync(committed);
        (await service.SaveAsync(1, Content())).IsSuccess.Should().BeFalse();
        storage.Verify(s => s.DeleteAsync("new", It.IsAny<CancellationToken>()), committed ? Times.Never() : Times.Once());
        storage.Verify(s => s.DeleteAsync("old", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Processing_Failure_Preserves_Current_Video()
    {
        storage.Setup(s => s.PrepareAsync(It.IsAny<UploadedWordVideo>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        (await service.SaveAsync(1, Content())).IsSuccess.Should().BeFalse();
        repository.Verify(r => r.ReplaceAsync(It.IsAny<uint>(), It.IsAny<StoredWordVideo>(), It.IsAny<CancellationToken>()), Times.Never);
        storage.Verify(s => s.DeleteAsync("new", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.DeleteAsync("old", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cleanup_And_Cache_Failures_Do_Not_Undo_Committed_Video()
    {
        cache.Setup(c => c.RemoveAsync(1, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        storage.Setup(s => s.DeleteAsync("old", It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        (await service.SaveAsync(1, Content())).IsSuccess.Should().BeTrue();
        storage.Verify(s => s.DeleteAsync("new", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Soft_Delete_Invalidates_Cache_And_Retains_Cloud_Asset()
    {
        repository.Setup(r => r.SoftDeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        (await service.DeleteAsync(1)).IsSuccess.Should().BeTrue();
        cache.Verify(c => c.RemoveAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UploadedContent Content() => new("swim.mp4", "video/mp4", 20 * 1024 * 1024, Stream.Null);
}
