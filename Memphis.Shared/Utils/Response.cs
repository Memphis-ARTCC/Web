namespace Memphis.Shared.Utils;

public class Response<T>
{
    public int StatusCode { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }
}

public class ResponsePaging<T>
{
    public int StatusCode { get; set; }
    public int ResultCount { get; set; }
    public int TotalCount { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }
}