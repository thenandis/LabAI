using System.Diagnostics.CodeAnalysis;

namespace SharpAPI.Application.Common;

/// <summary>
/// The Result pattern: an operation returns success OR an <see cref="Error"/>,
/// rather than throwing exceptions for expected outcomes.
/// </summary>
/// <remarks>
/// <para><b>Why not just throw NotFoundException?</b></para>
/// <list type="bullet">
///   <item><b>Explicit:</b> the method signature says "this can fail", and callers must handle it.</item>
///   <item><b>Performance:</b> exceptions are expensive (stack trace capture); a "product
///         not found" on a busy API shouldn't cost that.</item>
///   <item><b>Control flow:</b> exceptions are for EXCEPTIONAL things (database down,
///         bug). A missing record or invalid input is a normal business outcome.</item>
/// </list>
/// <para>
/// HOW THIS FITS: ProductService returns <c>Result&lt;ProductDto&gt;</c>, and the
/// endpoint turns success into 200/201/204 and failure into a ProblemDetails response.
/// </para>
/// </remarks>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    // MemberNotNullWhen tells the nullable analyser: "if IsSuccess is false, Error is not null".
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    // Implicit conversion lets a method declared as returning Result simply `return ProductErrors.NotFound(id);`
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Result that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, null) => _value = value;
    private Result(Error error) : base(false, error) { }

    /// <summary>The value. Throws if read on a failed result; that is a programming error.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);

    // `return dto;` and `return someError;` both compile in a method returning Result<T>.
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
