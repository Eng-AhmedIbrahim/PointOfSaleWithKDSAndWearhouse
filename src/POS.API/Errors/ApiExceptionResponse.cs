namespace POS.API.Errors;

public class ApiExceptionResponse:ApiResponse
{
    private readonly string? Details;

    public ApiExceptionResponse(int statusCode, string? message = null, string? details = null):base(statusCode,message)
    {
       Details = details;
    }
}