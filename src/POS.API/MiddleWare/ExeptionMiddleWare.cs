namespace POS.API.MiddleWare;

public class ExeptionMiddleWare
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExeptionMiddleWare> _logger;

    public ExeptionMiddleWare(RequestDelegate next,IHostEnvironment environment,ILogger<ExeptionMiddleWare> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = _environment.IsDevelopment() ?
                new ApiExceptionResponse((int)HttpStatusCode.InternalServerError, ex.Message, ex.StackTrace?.ToString()) :
                new ApiExceptionResponse((int)HttpStatusCode.InternalServerError, ex.Message);

            var options = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);

        }
    }
}
