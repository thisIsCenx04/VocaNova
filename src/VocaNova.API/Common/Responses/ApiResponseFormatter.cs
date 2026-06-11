using VocaNova.API.Common.Results;

namespace VocaNova.API.Common.Responses;

public static class ApiResponseFormatter
{
    public static ApiResponse<T> Success<T>(T? data, string? message = null)
    {
        return new ApiResponse<T>(
            success: true,
            data: data,
            message: message ?? "Success.",
            errors: Array.Empty<string>());
    }

    public static ApiResponse<T> Created<T>(T? data, string? message = null)
    {
        return new ApiResponse<T>(
            success: true,
            data: data,
            message: message ?? "Created.",
            errors: Array.Empty<string>());
    }

    public static ApiResponse<IReadOnlyCollection<T>> Paged<T>(
        PagedResult<T> result,
        string? message = null)
    {
        return new ApiResponse<IReadOnlyCollection<T>>(
            success: true,
            data: result.Items,
            message: message ?? "Success.",
            errors: Array.Empty<string>(),
            pagination: new PaginationMetadata(
                result.Page,
                result.Limit,
                result.TotalItems,
                result.TotalPages));
    }

    public static ApiResponse<object?> Error(
        string message,
        IEnumerable<string>? errors = null)
    {
        var errorList = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray()
            ?? Array.Empty<string>();

        return new ApiResponse<object?>(
            success: false,
            data: null,
            message: message,
            errors: errorList);
    }

    public static ApiResponse<T> FromResult<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return Success(result.Value, successMessage);
        }

        var message = result.Error ?? "Request failed.";

        return new ApiResponse<T>(
            success: false,
            data: default,
            message: message,
            errors: new[] { message });
    }
}
