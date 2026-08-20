namespace VocaNova.API.Features.AiGrading.BLL.Models;

public enum AiGradingErrorKind
{
    Validation,
}

public sealed class AiGradingOperationResult<T>
{
    private AiGradingOperationResult(T? value, string? error, AiGradingErrorKind? errorKind)
    {
        Value = value;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess => ErrorKind is null;
    public T? Value { get; }
    public string? Error { get; }
    public AiGradingErrorKind? ErrorKind { get; }

    public static AiGradingOperationResult<T> Success(T value) => new(value, null, null);
    public static AiGradingOperationResult<T> ValidationFailure(string error) =>
        new(default, error, AiGradingErrorKind.Validation);
}
