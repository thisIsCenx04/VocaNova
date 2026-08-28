namespace VocaNova.API.Features.Knn.BLL.Models;

public enum KnnErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict,
    TooManyRequests,
}

public sealed class KnnOperationResult<T>
{
    private KnnOperationResult(T? value, string? error, KnnErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public KnnErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            KnnErrorKind.Unauthorized => 401,
            KnnErrorKind.NotFound => 404,
            KnnErrorKind.Conflict => 409,
            KnnErrorKind.TooManyRequests => 429,
            null => 200,
            _ => 400,
        };

    public static KnnOperationResult<T> Success(T value) => new(value, null, null);

    public static KnnOperationResult<T> ValidationFailure(string error) =>
        new(default, error, KnnErrorKind.Validation);

    public static KnnOperationResult<T> Unauthorized(string error) =>
        new(default, error, KnnErrorKind.Unauthorized);

    public static KnnOperationResult<T> NotFound(string error) =>
        new(default, error, KnnErrorKind.NotFound);

    public static KnnOperationResult<T> Conflict(string error) =>
        new(default, error, KnnErrorKind.Conflict);

    public static KnnOperationResult<T> TooManyRequests(string error) =>
        new(default, error, KnnErrorKind.TooManyRequests);
}
