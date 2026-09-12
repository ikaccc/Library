namespace Library.Lending.Domain.Common;


public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("A failed result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;


    public Error? Error { get; }

    public static Result Success() => new(isSuccess: true, error: null);

    public static Result Failure(Error error) => new(isSuccess: false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, isSuccess: true, error: null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, isSuccess: false, error);

    public static implicit operator Result(Error error) => Failure(error);
}


public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }


    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error!.Code}: {Error.Message}).");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error!);
}
