using System.Linq.Expressions;
using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Progress.DAL.Mappings;

internal static class ProgressPersistenceMappings
{
    public static readonly Expression<Func<TestAnswer, ProgressAnswerStatistics>> ToAnswerStatistics =
        answer => new ProgressAnswerStatistics(
            answer.Session.StartedAt,
            answer.IsCorrect == true);

    public static readonly Expression<Func<UserWordProgress, WeakestWordStatistics>> ToWeakestWordStatistics =
        progress => new WeakestWordStatistics(
            progress.WordId,
            progress.Word.Word1,
            progress.Word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .ThenBy(sense => sense.SenseId)
                .Select(sense => sense.VietnameseMeaning)
                .FirstOrDefault(),
            progress.TestCount,
            progress.CorrectCount,
            progress.WrongCount,
            progress.MasteryLevel,
            progress.LastWrongAt,
            progress.NextReviewAt);

    public static readonly Expression<Func<UserWordProgress, WordProgressStatistics>> ToWordProgressStatistics =
        progress => new WordProgressStatistics(
            progress.WordId,
            progress.Word.Word1,
            progress.Word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .ThenBy(sense => sense.SenseId)
                .Select(sense => sense.VietnameseMeaning)
                .FirstOrDefault(),
            progress.TestCount,
            progress.CorrectCount,
            progress.WrongCount,
            progress.ConsecutiveCorrect,
            progress.IsInWrongList,
            progress.MasteryLevel,
            progress.SrsInterval,
            progress.EaseFactor,
            progress.LastTestedAt,
            progress.LastWrongAt,
            progress.NextReviewAt,
            progress.UpdatedAt);
}
