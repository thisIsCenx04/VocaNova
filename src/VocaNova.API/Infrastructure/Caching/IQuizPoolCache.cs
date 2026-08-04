using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

/// <summary>
/// Giữ lại tập từ ứng viên của một phiên kiểm tra. Tập này không được lưu vào
/// database, nên nếu không cache thì mỗi lần nộp đáp án phải dựng lại từ đầu.
/// </summary>
public interface IQuizPoolCache
{
    Task<IReadOnlyCollection<QuizPoolWordDto>?> GetAsync(
        uint sessionId,
        uint? listId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint sessionId,
        uint? listId,
        IReadOnlyCollection<QuizPoolWordDto> pool,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        uint sessionId,
        uint? listId,
        CancellationToken cancellationToken = default);
}
