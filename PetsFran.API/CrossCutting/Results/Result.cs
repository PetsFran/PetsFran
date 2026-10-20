namespace PetsFran.API.CrossCutting.Results;

/// <summary>
/// Result Pattern - Tratamento elegante de errors e sucesso
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }
    public List<string> Errors { get; private set; }

    protected Result(bool isSuccess, string message, List<string> errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public static Result Success(string message = "Operação realizada com sucesso")
        => new Result(true, message);

    public static Result Failure(string message, List<string> errors = null)
        => new Result(false, message, errors ?? new List<string> { message });

    public static Result Failure(List<string> errors)
        => new Result(false, "Uma ou mais validações falharam", errors);
}

/// <summary>
/// Result Pattern com dados genéricos
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; private set; }

    private Result(bool isSuccess, string message, T? data = default, List<string> errors = null)
        : base(isSuccess, message, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Operação realizada com sucesso")
        => new Result<T>(true, message, data);

    public static Result<T> Failure(string message, List<string> errors = null)
        => new Result<T>(false, message, default, errors ?? new List<string> { message });

    public static Result<T> Failure(List<string> errors)
        => new Result<T>(false, "Uma ou mais validações falharam", default, errors);
}

