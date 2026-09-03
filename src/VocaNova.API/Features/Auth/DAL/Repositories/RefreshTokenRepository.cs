using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.DAL.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public RefreshTokenRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task StageCreateAsync(CreateRefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = refreshToken.UserId,
            TokenHash = refreshToken.TokenHash,
            DeviceInfo = refreshToken.DeviceInfo,
            IpAddress = refreshToken.IpAddress,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedAt = refreshToken.CreatedAt,
        });
        return Task.CompletedTask;
    }

    public async Task<RefreshTokenRecord?> FindByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        var token = await TokenAggregate()
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        return token?.ToRefreshTokenRecord();
    }

    public async Task<RefreshTokenRecord?> FindForUpdateByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RefreshToken> query = _dbContext.RefreshTokens;
        if (_dbContext.Database.IsRelational())
        {
            query = _dbContext.RefreshTokens.FromSqlInterpolated(
                $"SELECT * FROM refresh_tokens WHERE token_hash = {hash} FOR UPDATE");
        }

        var token = await query
            .Include(token => token.User)
            .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        return token?.ToRefreshTokenRecord();
    }

    public async Task<bool> StageRevokeAsync(
        string hash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.RevokedAt = revokedAt;
        return true;
    }

    public async Task<int> StageRevokeAllAsync(
        uint userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
        }

        return tokens.Count;
    }

    private IQueryable<RefreshToken> TokenAggregate() =>
        _dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.Role);
}
