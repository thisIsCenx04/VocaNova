namespace VocaNova.API.Features.Dictionary.BLL.Models;

public enum DictionaryErrorKind
{
    Validation,
    NotFound,
    Conflict,
}

public sealed class DictionaryResult<T>
{
    private DictionaryResult(T? value, string? error, DictionaryErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;

    public T? Value { get; }

    public string? Error { get; }

    public DictionaryErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            DictionaryErrorKind.NotFound => 404,
            DictionaryErrorKind.Conflict => 409,
            null => 200,
            _ => 400,
        };

    public static DictionaryResult<T> Success(T value) => new(value, null, null);

    public static DictionaryResult<T> ValidationFailure(string error) =>
        new(default, error, DictionaryErrorKind.Validation);

    public static DictionaryResult<T> NotFound(string error) =>
        new(default, error, DictionaryErrorKind.NotFound);

    public static DictionaryResult<T> Conflict(string error) =>
        new(default, error, DictionaryErrorKind.Conflict);
}
