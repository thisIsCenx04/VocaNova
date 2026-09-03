using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface ISrsRepository
{
    Task<UserWordProgress?> FindAsync(uint userId, uint wordId, CancellationToken cancellationToken = default);
    void Stage(UserWordProgress progress);
}
