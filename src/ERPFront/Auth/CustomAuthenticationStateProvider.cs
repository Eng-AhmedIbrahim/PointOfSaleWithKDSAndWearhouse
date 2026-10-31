using System.IdentityModel.Tokens.Jwt;

namespace ERPFront.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly CommonProperties _commonProperties;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private bool _isInitialized = false;

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, CommonProperties commonProperties)
    {
        _jsRuntime = jsRuntime;
        _commonProperties = commonProperties;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(token))
            {
                _currentUser = ParseClaimsFromJwt(token);
                _commonProperties.AuthUser = _currentUser;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving token: {ex.Message}");
        }

        _isInitialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_commonProperties.AuthUser));
    }


    public async Task NotifyUserAuthentication(string token)
    {
        _currentUser = ParseClaimsFromJwt(token);
        _commonProperties.AuthUser = _currentUser; // Store in CommonProperties
        _isInitialized = true;

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task NotifyUserLogout()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        _commonProperties.AuthUser = _currentUser; // Reset in CommonProperties
        _isInitialized = false;

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    private ClaimsPrincipal ParseClaimsFromJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");

        return new ClaimsPrincipal(identity);
    }

    public async Task ReloadUserAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(token))
            {
                _currentUser = ParseClaimsFromJwt(token);
                _commonProperties.AuthUser = _currentUser;
            }
            else
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                _commonProperties.AuthUser = _currentUser;
            }
        }
        catch (Exception ex)
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            _commonProperties.AuthUser = _currentUser;
            Log.Error($"Error retrieving token: {ex.Message}");
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }


    public async Task SetTokenAsync(string token)
    {
        ClaimsPrincipal user;
        if (string.IsNullOrEmpty(token))
        {
            user = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            user = ParseClaimsFromJwt(token);
        }

        _commonProperties.AuthUser = user;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

}
