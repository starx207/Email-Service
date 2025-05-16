using System.Diagnostics.Contracts;

namespace EmailService.Models;

public readonly struct Result<T, E> {
    public readonly T Value;
    public readonly E Error;

    private Result(T v, E e, bool success) {
        Value = v;
        Error = e;
        Success = success;
    }

    public bool Success { get; }

    public static Result<T, E> Ok(T v) => new(v, default!, true);

    public static Result<T, E> Err(E e) => new(default!, e, false);

    public static implicit operator Result<T, E>(T v) => Ok(v);
    public static implicit operator Result<T, E>(E e) => Err(e);

    public R Match<R>(
            Func<T, R> success,
            Func<E, R> failure) =>
        Success ? success(Value) : failure(Error);
}
