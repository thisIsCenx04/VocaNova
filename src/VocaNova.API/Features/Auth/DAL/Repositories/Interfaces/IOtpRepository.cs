using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IOtpRepository
{
    Task<OtpRecord?> FindLatestAsync(
        string phone,
        string? purpose = null,
        uint? userId = null,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    Task<OtpRecord?> FindLatestForUpdateAsync(
        string phone,
        string? purpose = null,
        uint? userId = null,
        CancellationToken cancellationToken = default);

    Task StageCreateAsync(CreateOtpRecord otp, CancellationToken cancellationToken = default);

    Task StageUsedAsync(OtpRecord otp, uint? userId, DateTime usedAt, CancellationToken cancellationToken = default);

    Task StageAttemptAsync(uint otpId, int verifyAttemptCount, CancellationToken cancellationToken = default);
}
