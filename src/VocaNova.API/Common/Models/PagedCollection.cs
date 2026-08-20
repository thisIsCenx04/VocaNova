namespace VocaNova.API.Common.Models;

public sealed class PagedCollection<T>
{
    public PagedCollection(IReadOnlyCollection<T> items, int page, int limit, int totalItems)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);

        Items = items;
        Page = page;
        Limit = limit;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling(totalItems / (double)limit);
    }

    public IReadOnlyCollection<T> Items { get; }

    public int Page { get; }

    public int Limit { get; }

    public int TotalItems { get; }

    public int TotalPages { get; }
}
