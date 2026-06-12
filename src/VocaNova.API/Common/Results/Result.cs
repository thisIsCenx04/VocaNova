using Microsoft.AspNetCore.Http;

namespace VocaNova.API.Common.Results;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public string? Error { get; }

    public int StatusCode { get; }

    public static Result<T> Ok(T value)
    {
        return new Result<T>(true, value, null, StatusCodes.Status200OK);
    }

    public static Result<T> Ok()
    {
        return new Result<T>(true, default, null, StatusCodes.Status200OK);
    }

    public static Result<T> Fail(string error = "Bad request.")
    {
        return new Result<T>(false, default, error, StatusCodes.Status400BadRequest);
    }

    public static Result<T> NotFound(string error = "Resource not found.")
    {
        return new Result<T>(false, default, error, StatusCodes.Status404NotFound);
    }

    public static Result<T> Conflict(string error = "Conflict.")
    {
        return new Result<T>(false, default, error, StatusCodes.Status409Conflict);
    }

    public static Result<T> Unauthorized(string error = "Unauthorized.")
    {
        return new Result<T>(false, default, error, StatusCodes.Status401Unauthorized);
    }

    public static Result<T> Forbidden(string error = "Forbidden.")
    {
        return new Result<T>(false, default, error, StatusCodes.Status403Forbidden);
    }
}
