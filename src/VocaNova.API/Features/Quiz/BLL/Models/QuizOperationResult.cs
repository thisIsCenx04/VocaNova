namespace VocaNova.API.Features.Quiz.BLL.Models;

public enum QuizErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
    Forbidden,
    Conflict,
}

public sealed class QuizOperationResult<T>
{
    private QuizOperationResult(T? value, string? error, QuizErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;
    public T? Value { get; }
    public string? Error { get; }
    public QuizErrorKind? ErrorKind { get; }

    public int StatusCode =>
        ErrorKind switch
        {
            QuizErrorKind.Unauthorized => 401,
            QuizErrorKind.NotFound => 404,
            QuizErrorKind.Forbidden => 403,
            QuizErrorKind.Conflict => 409,
            null => 200,
            _ => 400,
        };

    public static QuizOperationResult<T> Success(T value) => new(value, null, null);
    public static QuizOperationResult<T> ValidationFailure(string error) => new(default, error, QuizErrorKind.Validation);
    public static QuizOperationResult<T> Unauthorized(string error) => new(default, error, QuizErrorKind.Unauthorized);
    public static QuizOperationResult<T> NotFound(string error) => new(default, error, QuizErrorKind.NotFound);
    public static QuizOperationResult<T> Forbidden(string error) => new(default, error, QuizErrorKind.Forbidden);
    public static QuizOperationResult<T> Conflict(string error) => new(default, error, QuizErrorKind.Conflict);
}
