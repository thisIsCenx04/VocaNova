using VocaNova.Dashboard.Models.Api.Users;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.Users;

public sealed class UserDetailViewModel
{
    public required AdminUserDetail Detail { get; init; }

    public PagedData<AdminUserTestSession> TestHistory { get; init; } = new();

    public PagedData<AuditLog> AuditLogs { get; init; } = new();
}
