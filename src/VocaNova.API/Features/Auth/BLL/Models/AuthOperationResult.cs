namespace VocaNova.API.Features.Auth.BLL.Models;

public enum AuthErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
}

public sealed class AuthOperationResult<T>
{
    private AuthOperationResult(T? value, string? error, AuthErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public AuthErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            AuthErrorKind.Unauthorized => 401,
            AuthErrorKind.Forbidden => 403,
            AuthErrorKind.NotFound => 404,
            AuthErrorKind.Conflict => 409,
            AuthErrorKind.TooManyRequests => 429,
            null => 200,
            _ => 400,
        };

    public static AuthOperationResult<T> Success(T value) => new(value, null, null);

    public static AuthOperationResult<T> ValidationFailure(string error) => new(default, error, AuthErrorKind.Validation);

    public static AuthOperationResult<T> Unauthorized(string error) => new(default, error, AuthErrorKind.Unauthorized);

    public static AuthOperationResult<T> Forbidden(string error) => new(default, error, AuthErrorKind.Forbidden);

    public static AuthOperationResult<T> NotFound(string error) => new(default, error, AuthErrorKind.NotFound);

    public static AuthOperationResult<T> Conflict(string error) => new(default, error, AuthErrorKind.Conflict);

    public static AuthOperationResult<T> TooManyRequests(string error) => new(default, error, AuthErrorKind.TooManyRequests);
}
