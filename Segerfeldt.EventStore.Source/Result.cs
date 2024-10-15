using System;

namespace Segerfeldt.EventStore.Source;

/// <summary>The result of an operation that may succeed or fail</summary>
/// <typeparam name="T">The type of the value if successful</typeparam>
public readonly struct Result<T>
{
    private readonly T? value;
    private readonly Exception? error;

    /// <summary>The error, assuming failure</summary>
    public string Error => error?.Message ?? "Unknown error";

    /// <summary>Whether this result is a failur</summary>
    public readonly bool IsFailure => value is null;
    /// <summary>Whether this result is successful</summary>
    public readonly bool IsSuccess => !IsFailure;

    private Result(T? value, Exception? error)
    {
        this.value = value;
        this.error = error;
    }

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(Failure failure) => new(default, failure.error);
    public static implicit operator Result(Result<T> result) => result.IsSuccess ? Result.Success : Result.Failure(result.error!);

    /// <summary>A successful result</summary>
    /// <param name="value">The resulting value</param>
    public static Result<T> Success(T value) => new(value, null);
    /// <summary>A failed result</summary>
    /// <param name="error">An exception that explains the error</param>
    public static Result<T> Failure(Exception error) => new(default, error);

    public Result<U> IfSuccess<U>(Func<T, Result<U>> conversion)
    {
        if (IsFailure) return new(default, error);
        else return conversion(value!);
    }

    /// <summary>Get the value or throw the error <see cref="Exception"/></summary>
    /// <exception cref="Exception">Thrown if failure</exception>
    /// <returns>The value</returns>
    public T OrThrow() => value ?? throw error ?? new Exception("Operation Failed");
}

/// <summary>The result of an operation that may succeed or fail</summary>
public readonly struct Result
{
    private readonly Exception? error;

    /// <summary>The error, assuming failure</summary>
    public string Error => error?.Message ?? "Unknown error";

    /// <summary>Whether this result is a failur</summary>
    public readonly bool IsFailure => error is not null;
    /// <summary>Whether this result is successful</summary>
    public readonly bool IsSuccess => !IsFailure;

    private Result(Exception? error) => this.error = error;

    public static implicit operator Result(Failure failure) => new(failure.error);

    /// <summary>A successful result</summary>
    public static readonly Result Success = new(null);

    /// <summary>A failed result</summary>
    /// <param name="error">An exception that explains the error</param>
    public static Result Failure(Exception error) => new(error);

    /// <summary>A failed result</summary>
    /// <param name="error">A essage that explans the error</param>
    public static Result Failure(string error) => Failure(new Exception(error));

    public Result<U> IfSuccess<U>(Func<Result<U>> conversion)
    {
        if (IsFailure) return Result<U>.Failure(error!);
        else return conversion();
    }
}

/// <summary>A result which is always a failure</summary>
public readonly struct Failure
{
    internal readonly Exception error;

    private Failure(Exception error) => this.error = error;

    /// <summary>A failure</summary>
    /// <param name="error">An exception that explains the error</param>
    public static Failure Error(Exception error) => new(error);

    /// <summary>A failure</summary>
    /// <param name="error">A essage that explans the error</param>
    public static Failure Error(string error) => Error(new Exception(error));
}
