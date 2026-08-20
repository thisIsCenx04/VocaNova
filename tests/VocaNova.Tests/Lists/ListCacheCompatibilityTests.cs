using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace VocaNova.Tests.Lists;

public class ListCacheCompatibilityTests
{
    [Fact]
    public void Redis_Cache_Should_Preserve_Key_And_Ttl()
    {
        var cache = CreateCache(new RedisSettings { InstanceName = "vocanova:" });

        var key = (RedisKey)typeof(RedisUserListCache)
            .GetMethod("GetKey", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(cache, new object[] { 7u })!;
        var ttl = (TimeSpan)typeof(RedisUserListCache)
            .GetField("ListsTtl", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;

        key.ToString().Should().Be("vocanova:user-lists:v2:7");
        ttl.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Redis_Entry_Should_Preserve_Legacy_SnakeCase_Payload()
    {
        var assembly = typeof(RedisUserListCache).Assembly;
        var entryType = assembly.GetType(
            "VocaNova.API.Infrastructure.Caching.Lists.UserListCacheEntry")!;
        var businessModel = new UserListSummary(3, "Travel", 2, new DateTime(2026, 1, 2));
        var entry = entryType.GetMethod("FromBusinessModel")!
            .Invoke(null, new object[] { businessModel });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            entry,
            entryType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        document.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal("list_id", "list_name", "word_count", "created_at");
    }

    [Fact]
    public async Task Redis_Unavailable_Should_Degrade_To_Null_And_NoOp()
    {
        var cache = CreateCache(new RedisSettings
        {
            Configuration = "127.0.0.1:1,abortConnect=true,connectTimeout=50,syncTimeout=50",
            InstanceName = "vocanova:",
        });

        var get = async () => await cache.GetAsync(7);
        var set = async () => await cache.SetAsync(
            7,
            new[] { new UserListSummary(3, "Travel", 2, default) });
        var remove = async () => await cache.RemoveAsync(7);

        (await get.Should().NotThrowAsync()).Which.Should().BeNull();
        await set.Should().NotThrowAsync();
        await remove.Should().NotThrowAsync();
    }

    private static RedisUserListCache CreateCache(RedisSettings settings) =>
        new(Options.Create(settings), NullLogger<RedisUserListCache>.Instance);
}
