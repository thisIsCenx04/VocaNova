using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizSessionRepository
{
    Task<QuizSessionDto> CreateAsync(
        uint userId,
        CreateSessionRequest request,
        int questionCount,
        CancellationToken cancellationToken = default);
}
