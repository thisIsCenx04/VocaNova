using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Services;

public interface IUserListService
{
    Task<Result<IReadOnlyCollection<UserListDto>>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserListDto>> CreateAsync(
        uint userId,
        CreateListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserListDto>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default);
}
