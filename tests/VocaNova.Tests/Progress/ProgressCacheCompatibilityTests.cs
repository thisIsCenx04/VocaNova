using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace VocaNova.Tests.Progress;

public class ProgressCacheCompatibilityTests
{
    [Fact]
    public void Redis_Cache_Should_Preserve_Key_Ttl_And_SnakeCase_Payload()
    {
        var cache = new RedisProgressSummaryCache(
            Options.Create(new RedisSettings { InstanceName = "vocanova:" }),
            NullLogger<RedisProgressSummaryCache>.Instance);
        var getKey = typeof(RedisProgressSummaryCache).GetMethod(
            "GetKey",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var key = (RedisKey)getKey.Invoke(cache, new object[] { 7u })!;
        var ttl = (TimeSpan)typeof(RedisProgressSummaryCache).GetField(
            "SummaryTtl",
            BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        key.ToString().Should().Be("vocanova:progress-summary:7");
        ttl.Should().Be(TimeSpan.FromMinutes(15));

        var entryType = typeof(RedisProgressSummaryCache).Assembly.GetType(
            "VocaNova.API.Infrastructure.Caching.Progress.ProgressSummaryCacheEntry")!;
        var summary = new ProgressSummary(2, 5, 75, 3, 4, 10, 2, 6);
        var entry = entryType.GetMethod("FromBusinessModel")!.Invoke(null, new object[] { summary });
        var json = JsonSerializer.Serialize(
            entry,
            entryType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        document.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal(
                "current_streak_days",
                "longest_streak_days",
                "accuracy_7d",
                "correct_7d",
                "total_answers_7d",
                "total_words_in_progress",
                "mastered_words",
                "sessions_this_month");
    }
}
