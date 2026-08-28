namespace VocaNova.API.Features.Progress.BLL.Models;

public enum ProgressErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
}

public sealed class ProgressResult<T>
{
    private ProgressResult(T? value, string? error, ProgressErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public ProgressErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            ProgressErrorKind.Unauthorized => 401,
            ProgressErrorKind.NotFound => 404,
            null => 200,
            _ => 400,
        };

    public static ProgressResult<T> Success(T value) => new(value, null, null);

    public static ProgressResult<T> ValidationFailure(string error) =>
        new(default, error, ProgressErrorKind.Validation);

    public static ProgressResult<T> Unauthorized(string error) =>
        new(default, error, ProgressErrorKind.Unauthorized);

    public static ProgressResult<T> NotFound(string error) =>
        new(default, error, ProgressErrorKind.NotFound);
}
