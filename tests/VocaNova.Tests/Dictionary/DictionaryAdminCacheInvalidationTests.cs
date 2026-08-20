using FluentAssertions;
using Moq;
using VocaNova.API.Common.Constants;

namespace VocaNova.Tests.Dictionary;

public sealed class DictionaryAdminCacheInvalidationTests
{
    [Fact]
    public async Task Word_Delete_Should_Invalidate_Detail_And_Each_Referencing_User_List()
    {
        var repository = new Mock<IWordAdminRepository>();
        repository.Setup(instance => instance.SetWordStatusAsync(7, UserStatus.Deleted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(instance => instance.GetReferencingUserIdsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([2u, 5u]);
        var detail = new Mock<IWordDetailCache>();
        var lists = new Mock<IUserListCache>();
        var service = new WordAdminService(repository.Object, detail.Object, userListCache: lists.Object);

        var result = await service.SoftDeleteAsync(7);

        result.IsSuccess.Should().BeTrue();
        detail.Verify(instance => instance.RemoveAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        lists.Verify(instance => instance.RemoveAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        lists.Verify(instance => instance.RemoveAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UserStatus.Deleted)]
    [InlineData(UserStatus.Active)]
    public async Task Sense_Status_Change_Should_Invalidate_Only_The_Word_Detail(string status)
    {
        var repository = new Mock<IWordAdminRepository>();
        repository.Setup(instance => instance.SetSenseStatusAsync(7, 9, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var detail = new Mock<IWordDetailCache>();
        var service = new WordAdminService(repository.Object, detail.Object);

        var result = status == UserStatus.Deleted
            ? await service.SoftDeleteSenseAsync(7, 9)
            : await service.RestoreSenseAsync(7, 9);

        result.IsSuccess.Should().BeTrue();
        detail.Verify(instance => instance.RemoveAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Topic_Delete_Should_Preserve_Current_Topics_Only_Invalidation()
    {
        var repository = new Mock<ITopicAdminRepository>();
        repository.Setup(instance => instance.SetStatusAsync(3, UserStatus.Deleted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var cache = new Mock<ITopicCache>();
        var service = new TopicAdminService(repository.Object, cache.Object);

        var result = await service.SoftDeleteAsync(3);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(instance => instance.RemoveTopicsAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(instance => instance.RemoveTopicWordsAsync(It.IsAny<uint>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
