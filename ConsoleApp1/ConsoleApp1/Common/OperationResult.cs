namespace ConsoleApp1.Common;

public class OperationResult
{
    public bool Success { get; }
    public string Message { get; }

    protected OperationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static OperationResult Ok(string message)
    {
        return new OperationResult(true, message);
    }

    public static OperationResult Fail(string message)
    {
        return new OperationResult(false, message);
    }
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; }

    private OperationResult(bool success, string message, T? data) : base(success, message)
    {
        Data = data;
    }

    public static OperationResult<T> Ok(T data, string message)
    {
        return new OperationResult<T>(true, message, data);
    }

    public new static OperationResult<T> Fail(string message)
    {
        return new OperationResult<T>(false, message, default);
    }
}
