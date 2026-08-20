using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed class PersonalTopicListRequest
{
    [JsonPropertyName("wordId")]
    public uint? WordId { get; set; }
}
