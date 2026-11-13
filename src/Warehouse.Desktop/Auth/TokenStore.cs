namespace Warehouse.Desktop.Auth;

/// <summary>
/// In-memory singleton that holds the current JWT token so that
/// AuthTokenHandler can inject it without needing JS interop.
/// </summary>
public class TokenStore
{
    private string? _token;

    public string? Token
    {
        get => _token;
        set => _token = value;
    }

    public bool HasToken => !string.IsNullOrWhiteSpace(_token);
}
