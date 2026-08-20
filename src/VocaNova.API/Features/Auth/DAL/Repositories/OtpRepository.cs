using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.DAL.Repositories;

public sealed class OtpRepository : IOtpRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public OtpRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpRecord?> FindLatestAsync(
        string phone,
        string? purpose = null,
        uint? userId = null,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var otp = await ApplyFilters(_dbContext.OtpVerifications, phone, userId, since)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return otp?.ToOtpRecord();
    }

    public async Task<OtpRecord?> FindLatestForUpdateAsync(
        string phone,
        string? purpose = null,
        uint? userId = null,
        CancellationToken cancellationToken = default)
    {
        var otp = await ApplyFilters(_dbContext.OtpVerifications, phone, userId, since: null)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return otp?.ToOtpRecord();
    }

    public Task StageCreateAsync(CreateOtpRecord otp, CancellationToken cancellationToken = default)
    {
        _dbContext.OtpVerifications.Add(new OtpVerification
        {
            UserId = otp.UserId,
            Phone = otp.Phone,
            OtpCode = otp.OtpCode,
            IsUsed = otp.IsUsed,
            Status = otp.Status,
            VerifyAttemptCount = otp.VerifyAttemptCount,
            ExpiresAt = otp.ExpiresAt,
            CreatedAt = otp.CreatedAt,
        });
        return Task.CompletedTask;
    }

    public async Task StageUsedAsync(
        OtpRecord record,
        uint? userId,
        DateTime usedAt,
        CancellationToken cancellationToken = default)
    {
        var otp = await _dbContext.OtpVerifications.SingleAsync(otp => otp.OtpId == record.OtpId, cancellationToken);
        otp.UserId = userId;
        otp.IsUsed = true;
        otp.VerifyAttemptCount = record.VerifyAttemptCount;
    }

    public async Task StageAttemptAsync(
        uint otpId,
        int verifyAttemptCount,
        CancellationToken cancellationToken = default)
    {
        var otp = await _dbContext.OtpVerifications.SingleAsync(otp => otp.OtpId == otpId, cancellationToken);
        otp.VerifyAttemptCount = verifyAttemptCount;
    }

    private static IQueryable<OtpVerification> ApplyFilters(
        IQueryable<OtpVerification> query,
        string phone,
        uint? userId,
        DateTime? since)
    {
        query = query.Where(otp => otp.Phone == phone && otp.Status == Common.Constants.OtpStatus.Active);
        query = query.Where(otp => otp.UserId == userId);
        if (since.HasValue)
        {
            query = query.Where(otp => otp.CreatedAt >= since.Value);
        }

        return query;
    }
}
