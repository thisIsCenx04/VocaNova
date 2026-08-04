namespace VocaNova.API.Infrastructure.Authentication;

public sealed record GoogleUserInfo(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? Picture);
