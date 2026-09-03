using System.Text.Json;
using FluentAssertions;
using VocaNova.API.Features.Quiz.Contracts.Requests;
using VocaNova.API.Features.Quiz.Contracts.Responses;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.Tests.Quiz;

/// <summary>
/// RedisQuizPoolCache lưu tập từ dưới dạng JSON và đọc lại vào một kiểu
/// interface. Vòng chuyển đổi đó chỉ hỏng lúc chạy thật, nên khoá nó lại ở đây
/// thay vì phải dựng Redis mới phát hiện.
/// </summary>
public class QuizPoolCacheSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void QuizPool_Should_Survive_A_Json_Round_Trip()
    {
        IReadOnlyCollection<QuizPoolWordDto> pool = new[]
        {
            new QuizPoolWordDto(4, new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), 3),
            new QuizPoolWordDto(3, new DateTime(2026, 1, 2, 9, 30, 0, DateTimeKind.Utc)),
        };

        var payload = JsonSerializer.Serialize(pool, JsonOptions);
        var restored = JsonSerializer.Deserialize<IReadOnlyCollection<QuizPoolWordDto>>(payload, JsonOptions);

        restored.Should().NotBeNull();
        restored.Should().BeEquivalentTo(pool, options => options.WithStrictOrdering());
    }

    [Fact]
    public void QuizPool_Round_Trip_Should_Keep_Default_WrongCount()
    {
        // WrongCount có giá trị mặc định trên record; nếu bị bỏ qua khi đọc lại
        // thì thứ tự "by_difficulty" sẽ sai âm thầm.
        IReadOnlyCollection<QuizPoolWordDto> pool = new[]
        {
            new QuizPoolWordDto(7, new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc)),
        };

        var payload = JsonSerializer.Serialize(pool, JsonOptions);
        var restored = JsonSerializer.Deserialize<IReadOnlyCollection<QuizPoolWordDto>>(payload, JsonOptions);

        restored.Should().NotBeNull();
        var word = restored!.Single();
        word.WordId.Should().Be(7);
        word.WrongCount.Should().Be(0);
        word.AddedAt.Should().Be(new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc));
    }
}
