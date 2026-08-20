using System.Text.Json.Serialization;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Infrastructure.Caching.Dictionary;

internal sealed record PagedCacheEntry<T>(
    T[] Items,
    int Page,
    int Limit,
    int TotalItems);

internal sealed record WordSummaryCacheEntry(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("image_url")] string? ImageUrl)
{
    public static WordSummaryCacheEntry FromBusinessModel(WordSummary word) =>
        new(word.WordId, word.Word, word.Phonetic, word.Cefr, word.PrimaryMeaning, word.ImageUrl);

    public WordSummary ToBusinessModel() =>
        new(WordId, Word, Phonetic, Cefr, PrimaryMeaning, ImageUrl);
}

internal sealed record TopicSummaryCacheEntry(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount)
{
    public static TopicSummaryCacheEntry FromBusinessModel(TopicSummary topic) =>
        new(topic.TopicId, topic.Name, topic.NameVi, topic.Icon, topic.WordCount);

    public TopicSummary ToBusinessModel() =>
        new(TopicId, Name, NameVi, Icon, WordCount);
}

internal sealed record WordDetailCacheEntry(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("word_key")] string WordKey,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("phonetic_us")] string? PhoneticUs,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("is_phrase")] bool IsPhrase,
    [property: JsonPropertyName("senses")] WordSenseCacheEntry[] Senses,
    [property: JsonPropertyName("examples")] WordExampleCacheEntry[] Examples,
    [property: JsonPropertyName("relations")] WordRelationCacheEntry[] Relations,
    [property: JsonPropertyName("audio")] WordAudioCacheEntry[] Audio,
    [property: JsonPropertyName("derived_forms")] WordDerivedFormCacheEntry[] DerivedForms,
    [property: JsonPropertyName("idioms")] WordIdiomCacheEntry[] Idioms,
    [property: JsonPropertyName("topics")] WordTopicCacheEntry[] Topics,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt)
{
    public static WordDetailCacheEntry FromBusinessModel(WordDetail word) =>
        new(
            word.WordId,
            word.Word,
            word.WordKey,
            word.Cefr,
            word.PhoneticUk,
            word.PhoneticUs,
            word.ImageUrl,
            word.IsPhrase,
            word.Senses.Select(WordSenseCacheEntry.FromBusinessModel).ToArray(),
            word.Examples.Select(WordExampleCacheEntry.FromBusinessModel).ToArray(),
            word.Relations.Select(WordRelationCacheEntry.FromBusinessModel).ToArray(),
            word.Audio.Select(WordAudioCacheEntry.FromBusinessModel).ToArray(),
            word.DerivedForms.Select(WordDerivedFormCacheEntry.FromBusinessModel).ToArray(),
            word.Idioms.Select(WordIdiomCacheEntry.FromBusinessModel).ToArray(),
            word.Topics.Select(WordTopicCacheEntry.FromBusinessModel).ToArray(),
            word.Status,
            word.CreatedAt,
            word.UpdatedAt);

    public WordDetail ToBusinessModel() =>
        new(
            WordId,
            Word,
            WordKey,
            Cefr,
            PhoneticUk,
            PhoneticUs,
            ImageUrl,
            IsPhrase,
            Senses.Select(item => item.ToBusinessModel()).ToArray(),
            Examples.Select(item => item.ToBusinessModel()).ToArray(),
            Relations.Select(item => item.ToBusinessModel()).ToArray(),
            Audio.Select(item => item.ToBusinessModel()).ToArray(),
            DerivedForms.Select(item => item.ToBusinessModel()).ToArray(),
            Idioms.Select(item => item.ToBusinessModel()).ToArray(),
            Topics.Select(item => item.ToBusinessModel()).ToArray(),
            Status,
            CreatedAt,
            UpdatedAt);
}

internal sealed record WordSenseCacheEntry(
    [property: JsonPropertyName("sense_id")] uint SenseId,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("word_class")] string WordClass,
    [property: JsonPropertyName("english_definition")] string EnglishDefinition,
    [property: JsonPropertyName("vietnamese_meaning")] string? VietnameseMeaning,
    [property: JsonPropertyName("examples")] WordExampleCacheEntry[] Examples,
    [property: JsonPropertyName("relations")] WordRelationCacheEntry[] Relations)
{
    public static WordSenseCacheEntry FromBusinessModel(WordSense sense) =>
        new(
            sense.SenseId,
            sense.Order,
            sense.WordClass,
            sense.EnglishDefinition,
            sense.VietnameseMeaning,
            sense.Examples.Select(WordExampleCacheEntry.FromBusinessModel).ToArray(),
            sense.Relations.Select(WordRelationCacheEntry.FromBusinessModel).ToArray());

    public WordSense ToBusinessModel() =>
        new(
            SenseId,
            Order,
            WordClass,
            EnglishDefinition,
            VietnameseMeaning,
            Examples.Select(item => item.ToBusinessModel()).ToArray(),
            Relations.Select(item => item.ToBusinessModel()).ToArray());
}

internal sealed record WordExampleCacheEntry(
    [property: JsonPropertyName("example_id")] uint ExampleId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("example_en")] string ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi,
    [property: JsonPropertyName("order")] int Order)
{
    public static WordExampleCacheEntry FromBusinessModel(WordExample example) =>
        new(example.ExampleId, example.SenseId, example.ExampleEn, example.ExampleVi, example.Order);

    public WordExample ToBusinessModel() =>
        new(ExampleId, SenseId, ExampleEn, ExampleVi, Order);
}

internal sealed record WordRelationCacheEntry(
    [property: JsonPropertyName("relation_id")] uint RelationId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("relation_type")] string RelationType,
    [property: JsonPropertyName("related_word")] string RelatedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("is_quiz_eligible")] bool IsQuizEligible)
{
    public static WordRelationCacheEntry FromBusinessModel(WordRelation relation) =>
        new(
            relation.RelationId,
            relation.SenseId,
            relation.RelationType,
            relation.RelatedWord,
            relation.LinkedWordId,
            relation.IsQuizEligible);

    public WordRelation ToBusinessModel() =>
        new(RelationId, SenseId, RelationType, RelatedWord, LinkedWordId, IsQuizEligible);
}

internal sealed record WordAudioCacheEntry(
    [property: JsonPropertyName("audio_id")] uint AudioId,
    [property: JsonPropertyName("accent")] string Accent,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("status")] string Status)
{
    public static WordAudioCacheEntry FromBusinessModel(WordAudio audio) =>
        new(audio.AudioId, audio.Accent, audio.Source, audio.Url, audio.Status);

    public WordAudio ToBusinessModel() => new(AudioId, Accent, Source, Url, Status);
}

internal sealed record WordDerivedFormCacheEntry(
    [property: JsonPropertyName("derived_id")] uint DerivedId,
    [property: JsonPropertyName("derived_word")] string DerivedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("word_class")] string? WordClass)
{
    public static WordDerivedFormCacheEntry FromBusinessModel(WordDerivedForm form) =>
        new(form.DerivedId, form.DerivedWord, form.LinkedWordId, form.WordClass);

    public WordDerivedForm ToBusinessModel() =>
        new(DerivedId, DerivedWord, LinkedWordId, WordClass);
}

internal sealed record WordIdiomCacheEntry(
    [property: JsonPropertyName("idiom_id")] uint IdiomId,
    [property: JsonPropertyName("idiom_text")] string IdiomText,
    [property: JsonPropertyName("meaning_en")] string? MeaningEn,
    [property: JsonPropertyName("meaning_vi")] string? MeaningVi)
{
    public static WordIdiomCacheEntry FromBusinessModel(WordIdiom idiom) =>
        new(idiom.IdiomId, idiom.IdiomText, idiom.MeaningEn, idiom.MeaningVi);

    public WordIdiom ToBusinessModel() => new(IdiomId, IdiomText, MeaningEn, MeaningVi);
}

internal sealed record WordTopicCacheEntry(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon)
{
    public static WordTopicCacheEntry FromBusinessModel(WordTopic topic) =>
        new(topic.TopicId, topic.Name, topic.NameVi, topic.Icon);

    public WordTopic ToBusinessModel() => new(TopicId, Name, NameVi, Icon);
}
