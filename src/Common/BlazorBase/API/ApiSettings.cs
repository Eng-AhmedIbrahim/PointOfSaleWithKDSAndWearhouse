namespace BlazorBase.API;

public record ApiSettings
{
    public string? ApiName { get; set; }
    public string? BaseUrl { get; set; }
    public ApiEndpoints? Endpoints { get; set; }
}