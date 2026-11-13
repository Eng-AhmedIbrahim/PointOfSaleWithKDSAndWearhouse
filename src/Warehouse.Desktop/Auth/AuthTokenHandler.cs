using Warehouse.Desktop.Services;

namespace Warehouse.Desktop.Auth;

/// <summary>
/// DelegatingHandler that automatically attaches the JWT Bearer token
/// to every outgoing HTTP request.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;

    public AuthTokenHandler(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("Authorization") && _tokenStore.HasToken)
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenStore.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
