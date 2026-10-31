namespace ERPFront.Components.Pages;

public partial class Login
{
    private string _pin = string.Empty;
    private string _userName = string.Empty;
    private ICollection<UserDto> Users = new List<UserDto>();
    private HttpClient? client;

    [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;


    private void AddDigit(string digit)
        => _pin += digit;

    private void ClearInput()
     => _pin = string.Empty;

    private void DeleteLastDigit()
    {
        if (_pin.Length > 0)
            _pin = _pin.Substring(0, _pin.Length - 1);
    }

    protected async override Task OnInitializedAsync()
    {
        client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
        var response = await client.GetAsync(ConstantStrings.GetAllUsersUrl) ?? new();
        if (response.IsSuccessStatusCode)
        {
            var usersString = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(usersString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            Users = users.OrderByDescending(u => u.UserName == "Administrator").ToList();
        }
        await base.OnInitializedAsync();
    }

    private async Task LoginAction()
    {
        var response = await client!.PostAsJsonAsync(ConstantStrings.LoginUserUrl, new { UserName = _userName, Password = _pin });

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<UserDto>();
            string token = responseContent?.Token ?? string.Empty;

            if (string.IsNullOrEmpty(token))
            {
                Snackbar.Add("Login Failed: No token received", Severity.Error);
                return;
            }

            _commonProperties.CurrentUser = _userName;
            _commonProperties.CurrentUserId = responseContent!.UserId;

            //Store token in local storage &update authentication state
            if (_authenticationStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
            {
                await customAuthStateProvider.NotifyUserAuthentication(token);
            }

            _navigationManager.NavigateTo("/pos", true);
        }
        else
        {
            Snackbar.Add("Login Failed", Severity.Error);
        }
    }




    private async Task<List<string>> GetUserPermissions()
    {
        var url = ConstantStrings.GetUserPermissionsUrl; // No userId
        var permissionsResponse = await client!.GetAsync(url);

        if (permissionsResponse.IsSuccessStatusCode)
        {
            var permissionsString = await permissionsResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(permissionsString) ?? new List<string>();
        }

        return new List<string>();
    }


    private void HandelOnChange(ChangeEventArgs e)
       => _userName = e.Value?.ToString() ?? string.Empty;
}