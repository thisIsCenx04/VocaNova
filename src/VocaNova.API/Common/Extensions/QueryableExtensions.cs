using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using VocaNova.API.Common.Results;

namespace VocaNova.API.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var skip = (page - 1) * limit;

        if (query.Provider is IAsyncQueryProvider)
        {
            var totalItems = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(skip)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, page, limit, totalItems);
        }

        var syncTotalItems = query.Count();
        var syncItems = query
            .Skip(skip)
            .Take(limit)
            .ToArray();

        return new PagedResult<T>(syncItems, page, limit, syncTotalItems);
    }
}
