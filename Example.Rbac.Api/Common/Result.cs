namespace Example.Rbac.Api.Common;

public record Result
{
    public bool IsFailure { get; set; }
    public Error? Error { get; set; }

    public static implicit operator Result(Error error) => new Result { IsFailure = true, Error = error };
}

public record Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(T data) => new Result<T> { IsFailure = false, Data = data };
    public static Result<T> Failure(Error error) => new Result<T> { IsFailure = true, Error = error };

    public static implicit operator Result<T>(T data) => Success(data);
}

public record Error(string Code, string Message);
