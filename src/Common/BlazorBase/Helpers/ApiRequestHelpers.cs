namespace BlazorBase.Helpers;

public static class ApiRequestHelpers
{
   
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public static async Task<HttpResponseMessage?> SendApiRequest(
        Func<Task<HttpResponseMessage>> apiRequest)
    {
        try
        {
            return await apiRequest();
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public static async Task<string> GetResponseMessage(
        HttpResponseMessage response, string successMessage)
    {
        var content = await response.Content.ReadAsStringAsync();

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
            case HttpStatusCode.NoContent:
            case HttpStatusCode.Created:
                return successMessage;
            case HttpStatusCode.BadRequest:
            case HttpStatusCode.NotFound:
                var errorResponse = DeserializeResponseContent<ApiResponse>(content);
                return $"{errorResponse?.Message ?? "Bad request or Not found"}";
            default:
                return $"Unexpected error: {response.StatusCode}";
        }
    }

    public static T? DeserializeResponseContent<T>(string content)
        => JsonSerializer.Deserialize<T>(content, Options);
}