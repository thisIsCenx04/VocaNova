using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VocaNova.API.Common.Models;
using VocaNova.API.Common.Constants;

namespace VocaNova.Tests.Notifications;

public class NotificationFeatureTests
{
    [Fact]
    public async Task Service_Should_Map_Deleted_Word_And_Preserve_Pagination()
    {
        var deletedAt = new DateTime(2026, 8, 10, 7, 30, 0, DateTimeKind.Utc);
        var repository = new Mock<INotificationRepository>();
        repository
            .Setup(instance => instance.ListDeletedWordsAsync(7, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedCollection<DeletedWordReference>(
                new[] { new DeletedWordReference(42, "orchard", deletedAt) },
                1,
                20,
                1));
        var service = new NotificationService(repository.Object);

        var result = await service.ListAsync(7, new NotificationListQuery(0, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
        result.Value.TotalItems.Should().Be(1);
        var notification = result.Value.Items.Should().ContainSingle().Subject;
        notification.NotificationId.Should().Be(42);
        notification.Type.Should().Be("word_deleted");
        notification.Title.Should().Be("Từ vựng đã bị gỡ");
        notification.Message.Should().Be(
            "Từ \"orchard\" đã bị gỡ khỏi từ điển. Nội dung liên quan trong danh sách của bạn có thể không còn khả dụng.");
        notification.ReferenceType.Should().Be("word");
        notification.ReferenceId.Should().Be(42);
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().Be(deletedAt);
        notification.ReadAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Service_Should_Reject_Invalid_Limit(int limit)
    {
        var repository = new Mock<INotificationRepository>(MockBehavior.Strict);
        var service = new NotificationService(repository.Object);

        var result = await service.ListAsync(7, new NotificationListQuery(1, limit));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Limit must be between 1 and 100.");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Repository_Should_Return_Only_Referenced_Deleted_Words_Newest_First()
    {
        await using var dbContext = CreateDbContext();
        var older = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = older.AddDays(1);
        dbContext.Words.AddRange(
            CreateWord(1, "active", UserStatus.Active, newer),
            CreateWord(2, "list word", UserStatus.Deleted, older),
            CreateWord(3, "progress word", UserStatus.Deleted, newer),
            CreateWord(4, "other user", UserStatus.Deleted, newer.AddDays(1)));
        dbContext.UserListWords.Add(new UserListWord
        {
            UserId = 7,
            ListId = 10,
            WordId = 2,
            AddMethod = "manual",
            AddedAt = older,
            Status = UserStatus.Active,
        });
        dbContext.UserWordProgresses.AddRange(
            CreateProgress(1, 7, 3, newer),
            CreateProgress(2, 99, 4, newer));
        await dbContext.SaveChangesAsync();
        var repository = new NotificationRepository(dbContext);

        var result = await repository.ListDeletedWordsAsync(7, 1, 20);

        result.TotalItems.Should().Be(2);
        result.Items.Select(item => item.WordId).Should().Equal(3, 2);
    }

    [Fact]
    public async Task Controller_Should_Keep_Public_Json_Contract_And_Pagination_Envelope()
    {
        var createdAt = new DateTime(2026, 8, 10, 7, 30, 0, DateTimeKind.Utc);
        var service = new Mock<INotificationService>();
        service
            .Setup(instance => instance.ListAsync(7, new NotificationListQuery(2, 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationListResult.Success(
                new PagedCollection<Notification>(
                    new[]
                    {
                        new Notification(
                            42,
                            "word_deleted",
                            "Từ vựng đã bị gỡ",
                            "message",
                            "word",
                            42,
                            false,
                            createdAt,
                            null),
                    },
                    2,
                    10,
                    11)));
        var controller = CreateController(service.Object, "7");

        var action = await controller.List(
            new NotificationListRequest { Page = 2, Limit = 10 },
            CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(
            ok.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be("Notifications loaded successfully.");
        var item = root.GetProperty("data")[0];
        item.EnumerateObject().Select(property => property.Name).Should().Equal(
            "notification_id",
            "type",
            "title",
            "message",
            "ref_type",
            "ref_id",
            "is_read",
            "created_at",
            "read_at");
        root.GetProperty("pagination").GetProperty("page").GetInt32().Should().Be(2);
        root.GetProperty("pagination").GetProperty("totalPages").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Controller_Should_Return_Unauthorized_When_User_Claim_Is_Missing()
    {
        var service = new Mock<INotificationService>(MockBehavior.Strict);
        var controller = CreateController(service.Object, null);

        var action = await controller.List(new NotificationListRequest(), CancellationToken.None);

        action.Should().BeOfType<UnauthorizedObjectResult>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controller_Should_Map_Bll_Validation_Failure_To_Existing_Bad_Request_Envelope()
    {
        var service = new Mock<INotificationService>();
        service
            .Setup(instance => instance.ListAsync(7, new NotificationListQuery(1, 101), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationListResult.ValidationFailure(
                "Limit must be between 1 and 100."));
        var controller = CreateController(service.Object, "7");

        var action = await controller.List(
            new NotificationListRequest { Page = 1, Limit = 101 },
            CancellationToken.None);

        var badRequest = action.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(
            badRequest.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("message").GetString().Should().Be("Limit must be between 1 and 100.");
        root.GetProperty("errors")[0].GetString().Should().Be("Limit must be between 1 and 100.");
    }

    private static NotificationsController CreateController(
        INotificationService service,
        string? userId)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim("user_id", userId) };
        var controller = new NotificationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                },
            },
        };

        return controller;
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VocaNovaDbContext(options);
    }

    private static Word CreateWord(
        uint wordId,
        string text,
        string status,
        DateTime updatedAt) =>
        new()
        {
            WordId = wordId,
            Word1 = text,
            WordKey = text,
            Status = status,
            CreatedAt = updatedAt.AddDays(-1),
            UpdatedAt = updatedAt,
        };

    private static UserWordProgress CreateProgress(
        uint progressId,
        uint userId,
        uint wordId,
        DateTime updatedAt) =>
        new()
        {
            ProgressId = progressId,
            UserId = userId,
            WordId = wordId,
            EaseFactor = 2.5f,
            UpdatedAt = updatedAt,
        };
}
