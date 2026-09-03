namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record CreateListCommand(string ListName);

public sealed record UpdateListCommand(string ListName);

public sealed record AddListWordCommand(uint WordId, string AddMethod, string? Note);

public sealed record AddRandomListWordsCommand(uint? TopicId, int Count, string? Method);

public sealed record UpdateListWordNoteCommand(string? Note);

public sealed record AddPersonalTopicWordCommand(uint WordId, string? Note);
