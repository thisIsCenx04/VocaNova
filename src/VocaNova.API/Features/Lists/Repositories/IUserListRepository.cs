using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Repositories;

public interface IUserListRepository
{
    Task<IReadOnlyCollection<UserListDto>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<bool> ListNameExistsAsync(
        uint userId,
        string normalizedListName,
        uint? excludingListId = null,
        CancellationToken cancellationToken = default);

    Task<UserListDto> CreateAsync(
        uint userId,
        string listName,
        CancellationToken cancellationToken = default);

    Task<UserListOwnershipDto?> FindOwnershipAsync(
        uint listId,
        CancellationToken cancellationToken = default);

    Task<UserListDto?> UpdateAsync(
        uint listId,
        string listName,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteWithWordsAsync(
        uint listId,
        CancellationToken cancellationToken = default);
}
