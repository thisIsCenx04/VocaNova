using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Infrastructure.Caching.Quiz;

public sealed record QuizPoolCacheEntry(uint WordId, DateTime AddedAt, int WrongCount = 0)
{
    public static QuizPoolCacheEntry From(QuizPoolWord word) => new(word.WordId, word.AddedAt, word.WrongCount);
    public QuizPoolWord ToBusinessModel() => new(WordId, AddedAt, WrongCount);
}
