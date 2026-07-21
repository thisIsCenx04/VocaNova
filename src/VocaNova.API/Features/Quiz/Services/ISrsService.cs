using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface ISrsService
{
    /// <summary>
    /// Chỉ dựng thay đổi trong bộ nhớ; người gọi chịu trách nhiệm lưu để tiến
    /// độ SRS nằm cùng transaction với câu trả lời sinh ra nó. Với bản ghi mới,
    /// <c>ProgressId</c> trong kết quả trả về chỉ có giá trị sau khi đã lưu.
    /// </summary>
    Task<Result<UserWordProgressDto>> UpdateProgressAsync(
        uint userId,
        uint wordId,
        bool isCorrect,
        CancellationToken cancellationToken = default);
}
