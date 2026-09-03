namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record AdminTopic(
    uint TopicId,
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    string Status,
    int WordCount);
