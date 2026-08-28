using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Admin.DAL.Repositories;

public sealed class AdminStatsRepository : IAdminStatsRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AdminStatsRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardStatsModel> GetDashboardStatsAsync(
        DateTime todayUtc,
        DateTime tomorrowUtc,
        DateTime sevenDayStartUtc,
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(user => user.Status != UserStatus.Deleted, cancellationToken);
        var totalWords = await _dbContext.Words
            .AsNoTracking()
            .CountAsync(cancellationToken);
        var sessionsToday = await _dbContext.TestSessions
            .AsNoTracking()
            .CountAsync(session =>
                session.Status == TestSessionStatus.Completed
                && session.StartedAt >= todayUtc
                && session.StartedAt < tomorrowUtc,
                cancellationToken);
        var sevenDaySessions = _dbContext.TestSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == TestSessionStatus.Completed
                && session.StartedAt >= sevenDayStartUtc
                && session.StartedAt < tomorrowUtc);
        var correctCount = await sevenDaySessions
            .SumAsync(session => (int?)session.CorrectCount, cancellationToken)
            ?? 0;
        var wrongCount = await sevenDaySessions
            .SumAsync(session => (int?)session.WrongCount, cancellationToken)
            ?? 0;

        return new AdminDashboardStatsModel(
            totalUsers,
            totalWords,
            sessionsToday,
            ToAccuracy(correctCount, wrongCount));
    }

    public async Task<AdminDemographicsModel> GetDemographicsAsync(CancellationToken cancellationToken = default)
    {
        var ageRanges = await (
                from profile in ActiveLearningProfiles()
                join ageRange in _dbContext.AgeRanges.AsNoTracking()
                    on profile.AgeRangeId equals ageRange.AgeRangeId
                group profile by new { ageRange.AgeRangeId, ageRange.Name } into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new AdminDemographicGroupModel(
                    grouped.Key.AgeRangeId,
                    grouped.Key.Name,
                    grouped.Count()))
            .ToListAsync(cancellationToken);

        var occupations = await (
                from profile in ActiveLearningProfiles()
                join occupation in _dbContext.Occupations.AsNoTracking()
                    on profile.OccupationId equals occupation.OccupationId
                group profile by new { occupation.OccupationId, occupation.Name } into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new AdminDemographicGroupModel(
                    grouped.Key.OccupationId,
                    grouped.Key.Name,
                    grouped.Count()))
            .ToListAsync(cancellationToken);

        var educationLevels = await (
                from profile in ActiveLearningProfiles()
                join educationLevel in _dbContext.EducationLevels.AsNoTracking()
                    on profile.EducationLevelId equals educationLevel.EducationLevelId
                group profile by new { educationLevel.EducationLevelId, educationLevel.Name } into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new AdminDemographicGroupModel(
                    grouped.Key.EducationLevelId,
                    grouped.Key.Name,
                    grouped.Count()))
            .ToListAsync(cancellationToken);

        return new AdminDemographicsModel(ageRanges, occupations, educationLevels);
    }

    public async Task<IReadOnlyCollection<AdminWrongWordModel>> GetTopWrongWordsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TestAnswers
            .AsNoTracking()
            .Where(answer =>
                answer.IsCorrect != null
                && answer.Session.Status == TestSessionStatus.Completed)
            .GroupBy(answer => new { answer.WordId, Word = answer.Word.Word1 })
            .Select(grouped => new
            {
                grouped.Key.WordId,
                grouped.Key.Word,
                WrongCount = grouped.Sum(answer => answer.IsCorrect == false ? 1 : 0),
                CorrectCount = grouped.Sum(answer => answer.IsCorrect == true ? 1 : 0),
                TotalCount = grouped.Count(),
            })
            .Where(row => row.WrongCount > 0)
            .OrderByDescending(row => row.WrongCount)
            .ThenByDescending(row => row.TotalCount)
            .ThenBy(row => row.Word)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new AdminWrongWordModel(
                row.WordId,
                row.Word,
                row.WrongCount,
                row.CorrectCount,
                row.TotalCount,
                ToAccuracy(row.CorrectCount, row.WrongCount)))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AdminSessionAccuracyRow>> GetSessionAccuracyRowsAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _dbContext.TestSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == TestSessionStatus.Completed
                && session.StartedAt >= fromInclusiveUtc
                && session.StartedAt < toExclusiveUtc)
            .Select(session => new
            {
                session.StartedAt,
                session.CorrectCount,
                session.WrongCount,
            })
            .ToListAsync(cancellationToken);

        return sessions
            .GroupBy(session => DateOnly.FromDateTime(session.StartedAt))
            .Select(grouped => new AdminSessionAccuracyRow(
                grouped.Key,
                grouped.Sum(session => session.CorrectCount),
                grouped.Sum(session => session.WrongCount)))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AdminSessionCountRow>> GetSessionCountsByDayAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var startedDates = await _dbContext.TestSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == TestSessionStatus.Completed
                && session.StartedAt >= fromInclusiveUtc
                && session.StartedAt < toExclusiveUtc)
            .Select(session => session.StartedAt)
            .ToListAsync(cancellationToken);

        return startedDates
            .GroupBy(startedAt => DateOnly.FromDateTime(startedAt))
            .Select(grouped => new AdminSessionCountRow(grouped.Key, grouped.Count()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AdminMasteryCountRow>> GetMasteryDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.UserWordProgresses
            .AsNoTracking()
            .Where(progress => progress.User.Status != UserStatus.Deleted)
            .GroupBy(progress => progress.MasteryLevel)
            .Select(grouped => new AdminMasteryCountRow(grouped.Key, grouped.Count()))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public Task<PagedResult<AdminAuditLogModel>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = _dbContext.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (query.UserId is not null)
        {
            source = source.Where(log => log.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            source = source.Where(log => log.EntityType == query.Entity);
        }

        return source
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.LogId)
            .Select(log => new AdminAuditLogModel(
                log.LogId,
                log.UserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.PayloadBefore,
                log.PayloadAfter,
                log.IpAddress,
                log.CreatedAt))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    private IQueryable<Infrastructure.Persistence.Entities.UserLearningProfile> ActiveLearningProfiles()
    {
        return _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(profile => profile.User.Status != UserStatus.Deleted);
    }

    private static double ToAccuracy(int correctCount, int wrongCount)
    {
        var total = correctCount + wrongCount;
        return total == 0 ? 0 : Math.Round(correctCount * 100d / total, 2);
    }
}
