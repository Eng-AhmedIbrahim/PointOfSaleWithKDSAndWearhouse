namespace BlazorBase.API;

public class ApiResponse
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }

    public ApiResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message;
    }

    public ApiResponse() {}
}

public class ApiValidationErrorResponse : ApiResponse
{
    public IEnumerable<string>? Errors { get; set; }
    public ApiValidationErrorResponse() : base(400) {}
}