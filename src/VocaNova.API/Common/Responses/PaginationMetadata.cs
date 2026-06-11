namespace VocaNova.API.Common.Responses;

public sealed class PaginationMetadata
{
    public PaginationMetadata(int page, int limit, int totalItems, int totalPages)
    {
        Page = page;
        Limit = limit;
        TotalItems = totalItems;
        TotalPages = totalPages;
    }

    public int Page { get; }

    public int Limit { get; }

    public int TotalItems { get; }

    public int TotalPages { get; }
}
