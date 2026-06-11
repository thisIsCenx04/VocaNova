namespace VocaNova.API.Common.Responses;

public sealed class ApiResponse<T>
{
    public ApiResponse(
        bool success,
        T? data,
        string? message,
        IReadOnlyCollection<string> errors,
        PaginationMetadata? pagination = null)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
        Pagination = pagination;
    }

    public bool Success { get; }

    public T? Data { get; }

    public string? Message { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public PaginationMetadata? Pagination { get; }
}
