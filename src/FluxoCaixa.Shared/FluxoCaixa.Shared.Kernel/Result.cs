namespace FluxoCaixa.Shared.Kernel;

public enum ErrorType { Validation, NotFound, BusinessRule, Unauthorized, Conflict, Internal }

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error, ErrorType type = Kernel.ErrorType.BusinessRule)
        => new(false, default, error, type);

    public static Result<T> NotFound(string entity)
        => new(false, default, $"{entity} não encontrado", Kernel.ErrorType.NotFound);

    public static Result<T> Unauthorized(string message = "Não autorizado")
        => new(false, default, message, Kernel.ErrorType.Unauthorized);

    public static Result<T> Conflict(string message)
        => new(false, default, message, Kernel.ErrorType.Conflict);

    public bool IsFailure => !IsSuccess;

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!, ErrorType!.Value);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, ErrorType type = ErrorType.BusinessRule)
        => Result<T>.Failure(error, type);
}
