using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RedisTopicCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisTopicCache;
using RedisWordDetailCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordDetailCache;
using RedisWordSearchCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordSearchCache;

namespace VocaNova.Tests.Dictionary;

public class DictionaryCacheCompatibilityTests
{
    [Fact]
    public void Redis_Caches_Should_Preserve_Keys_And_Ttls()
    {
        var settings = Options.Create(new RedisSettings { InstanceName = "vocanova:" });
        var search = new RedisWordSearchCache(settings, NullLogger<RedisWordSearchCache>.Instance);
        var detail = new RedisWordDetailCache(settings, NullLogger<RedisWordDetailCache>.Instance);
        var topic = new RedisTopicCache(settings, NullLogger<RedisTopicCache>.Instance);

        InvokeKey(search, "word-search:run:1:20:A1:_:_").Should()
            .Be("vocanova:word-search:run:1:20:A1:_:_");
        InvokeKey(detail, 7u).Should().Be("vocanova:word:7");
        InvokeKey(topic, "topics").Should().Be("vocanova:topics");
        var topicWordsKey = (string)typeof(RedisTopicCache).GetMethod(
            "GetTopicWordsKey",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { 3u, 2, 10 })!;
        topicWordsKey.Should().Be("topic-words:3:2:10");

        ReadTtl<RedisWordSearchCache>("SearchTtl").Should().Be(TimeSpan.FromMinutes(5));
        ReadTtl<RedisWordDetailCache>("DetailTtl").Should().Be(TimeSpan.FromMinutes(30));
        ReadTtl<RedisTopicCache>("TopicsTtl").Should().Be(TimeSpan.FromMinutes(60));
        ReadTtl<RedisTopicCache>("TopicWordsTtl").Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Redis_Entries_Should_Preserve_Legacy_SnakeCase_Payloads()
    {
        var assembly = typeof(RedisWordDetailCache).Assembly;
        var summaryJson = SerializeCacheEntry(
            assembly,
            "WordSummaryCacheEntry",
            new WordSummary(7, "run", "/rʌn/", "A1", "chạy", null));
        summaryJson.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal("word_id", "word", "phonetic", "cefr", "primary_meaning", "image_url");

        var topicJson = SerializeCacheEntry(
            assembly,
            "TopicSummaryCacheEntry",
            new TopicSummary(3, "Sports", "Thể thao", "ball", 4));
        topicJson.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal("topic_id", "name", "name_vi", "icon", "word_count");

        var detail = new WordDetail(
            7,
            "run",
            "run",
            "A1",
            null,
            "/rʌn/",
            null,
            false,
            Array.Empty<WordSense>(),
            Array.Empty<WordExample>(),
            Array.Empty<WordRelation>(),
            Array.Empty<WordAudio>(),
            Array.Empty<WordDerivedForm>(),
            Array.Empty<WordIdiom>(),
            Array.Empty<WordTopic>(),
            "active",
            default,
            default);
        var detailJson = SerializeCacheEntry(assembly, "WordDetailCacheEntry", detail);
        detailJson.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal(
                "word_id", "word", "word_key", "cefr", "phonetic_uk", "phonetic_us",
                "image_url", "is_phrase", "senses", "examples", "relations", "audio",
                "derived_forms", "idioms", "topics", "status", "created_at", "updated_at");
    }

    private static string InvokeKey(object cache, object argument)
    {
        var key = (RedisKey)cache.GetType().GetMethod(
            "GetKey",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cache, new[] { argument })!;
        return key.ToString();
    }

    private static TimeSpan ReadTtl<T>(string fieldName) =>
        (TimeSpan)typeof(T).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    private static JsonDocument SerializeCacheEntry(
        Assembly assembly,
        string entryName,
        object businessModel)
    {
        var entryType = assembly.GetType(
            $"VocaNova.API.Infrastructure.Caching.Dictionary.{entryName}")!;
        var entry = entryType.GetMethod("FromBusinessModel")!
            .Invoke(null, new[] { businessModel });
        var json = JsonSerializer.Serialize(
            entry,
            entryType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonDocument.Parse(json);
    }
}
