using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface ISrsRepository
{
    Task<UserWordProgress?> FindAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);

    void Add(UserWordProgress progress);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
