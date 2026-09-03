using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Features.Progress.Contracts.Requests;
using VocaNova.API.Features.Progress.Contracts.Responses;

namespace VocaNova.API.Features.Progress.Mappings;

public static class ProgressMappings
{
    public static ProgressChartQuery ToBusinessQuery(this ProgressChartRequest request) =>
        new(request.Granularity);

    public static WeakestWordsQuery ToBusinessQuery(this WeakestWordsRequest request) =>
        new(request.Limit);

    public static ProgressSummaryResponse ToResponse(this ProgressSummary summary) =>
        new(
            summary.CurrentStreakDays,
            summary.LongestStreakDays,
            summary.Accuracy7Days,
            summary.Correct7Days,
            summary.TotalAnswers7Days,
            summary.TotalWordsInProgress,
            summary.MasteredWords,
            summary.SessionsThisMonth);

    public static ProgressChartResponse ToResponse(this ProgressChart chart) =>
        new(
            chart.Granularity,
            chart.Points.Select(point => new ProgressChartPointResponse(
                point.PeriodStart,
                point.PeriodEnd,
                point.PeriodLabel,
                point.SessionsCount,
                point.CorrectCount,
                point.TotalAnswers,
                point.Accuracy)).ToArray());

    public static IReadOnlyCollection<MasteryBreakdownResponse> ToResponse(
        this IReadOnlyCollection<MasteryBreakdown> breakdown) =>
        breakdown
            .Select(item => new MasteryBreakdownResponse(item.MasteryLevel, item.WordCount))
            .ToArray();

    public static IReadOnlyCollection<WeakestWordResponse> ToResponse(
        this IReadOnlyCollection<WeakestWord> words) =>
        words.Select(word => new WeakestWordResponse(
            word.WordId,
            word.Word,
            word.PrimaryMeaning,
            word.TestCount,
            word.CorrectCount,
            word.WrongCount,
            word.AccuracyRate,
            word.MasteryLevel,
            word.LastWrongAt,
            word.NextReviewAt)).ToArray();

    public static WordProgressResponse ToResponse(this WordProgress progress) =>
        new(
            progress.WordId,
            progress.Word,
            progress.PrimaryMeaning,
            progress.TestCount,
            progress.CorrectCount,
            progress.WrongCount,
            progress.AccuracyRate,
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
