using System.Diagnostics.CodeAnalysis;

namespace WarehouseERP.SharedKernel.Results;

/// <summary>
/// Outcome of an operation that can fail in an expected way. Unexpected failures stay exceptions.
/// </summary>
public class Result
{
    private static readonly Result SuccessInstance = new(null);

    protected Result(Error? error) => Error = error;

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>The failure; null on success.</summary>
    public Error? Error { get; }

    public static Result Success() => SuccessInstance;

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Success<T>(T value) => Result<T>.FromValue(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.FromError(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome carrying a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    /// <summary>The value; reading it from a failure is a programming error.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value ({Error}).");

    internal static Result<T> FromValue(T value) => new(value, null);

    internal static Result<T> FromError(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => FromValue(value);

    public static implicit operator Result<T>(Error error) => FromError(error);
}
