namespace VocaNova.API.Features.Lists.BLL.Models;

public enum ListErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
    Forbidden,
    Conflict,
}

public sealed class ListResult<T>
{
    private ListResult(T? value, string? error, ListErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public ListErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            ListErrorKind.Unauthorized => 401,
            ListErrorKind.NotFound => 404,
            ListErrorKind.Forbidden => 403,
            ListErrorKind.Conflict => 409,
            null => 200,
            _ => 400,
        };

    public static ListResult<T> Success(T value) => new(value, null, null);

    public static ListResult<T> ValidationFailure(string error) =>
        new(default, error, ListErrorKind.Validation);

    public static ListResult<T> Unauthorized(string error) =>
        new(default, error, ListErrorKind.Unauthorized);

    public static ListResult<T> NotFound(string error) =>
        new(default, error, ListErrorKind.NotFound);

    public static ListResult<T> Forbidden(string error) =>
        new(default, error, ListErrorKind.Forbidden);

    public static ListResult<T> Conflict(string error) =>
        new(default, error, ListErrorKind.Conflict);
}

public enum ListLookupErrorKind
{
    ListNotFound,
    ListForbidden,
    WordNotFound,
    TopicNotFound,
}

public sealed class ListLookupResult<T>
{
    private ListLookupResult(T? value, string? error, ListLookupErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public ListLookupErrorKind? ErrorKind { get; }

    public static ListLookupResult<T> Success(T value) => new(value, null, null);

    public static ListLookupResult<T> ListNotFound() =>
        new(default, "List lookup did not find an accessible list.", ListLookupErrorKind.ListNotFound);

    public static ListLookupResult<T> ListForbidden() =>
        new(default, "List lookup found a foreign list.", ListLookupErrorKind.ListForbidden);

    public static ListLookupResult<T> WordNotFound() =>
        new(default, "Word lookup failed.", ListLookupErrorKind.WordNotFound);

    public static ListLookupResult<T> TopicNotFound() =>
        new(default, "Topic lookup failed.", ListLookupErrorKind.TopicNotFound);
}
