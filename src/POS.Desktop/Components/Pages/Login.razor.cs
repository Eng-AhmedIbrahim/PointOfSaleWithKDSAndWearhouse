using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace POS.Desktop.Components.Pages;

public partial class Login
{
    private string _pin = string.Empty;
    private string _userName = string.Empty;
    private ICollection<UserDto> Users = new List<UserDto>();
    private HttpClient? client;
    private ElementReference pinInput;

    [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;
    [Inject] private IPosFeatureSettingsService _featureSettingsService { get; set; } = default!;
    [Inject] private IOrderSettingsService _orderSettingsService { get; set; } = default!;
    [Inject] private IAppDateService _appDateService { get; set; } = default!;
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
        // Clear any existing items to avoid NavigationLock behavior on login
        _commonProperties.TableItems = new List<TableItem>();
        _commonProperties.AppendedTableItems = new List<TableItem>();
        _commonProperties.CurrentDineInOrder = null;
        _commonProperties.DineInOrdersDetails = new Dictionary<int, List<DineInOrderDetails>>();
        _commonProperties.DineInOrderValues = new();
        _commonProperties.UpdateDineInOrder = false;
        
        try
        {
            client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
            var response = await client.GetAsync(ConstantStrings.GetAllUsersUrl);
            if (response != null && response.IsSuccessStatusCode)
            {
                var usersString = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDto>>(usersString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                Users = users.OrderByDescending(u => u.UserName == "Administrator").ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load users. Please check connection.", Severity.Error);
        }
        await base.OnInitializedAsync();
    }

    private async Task LoginAction()
    {
        if (string.IsNullOrEmpty(_userName) || _userName == "Select User")
        {
            Snackbar.Add("Please select a user first", Severity.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_pin))
        {
            Snackbar.Add("Please enter your PIN", Severity.Warning);
            return;
        }

        try
        {
            var response = await client!.PostAsJsonAsync(ConstantStrings.LoginUserUrl, new { UserName = _userName, Password = _pin, ForBackOffice = false });

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

                // Load settings for startup screen
                if (_commonProperties.FeatureSettings == null || !_commonProperties.FeatureSettings.Any())
                    _commonProperties.FeatureSettings = await _featureSettingsService.GetSettingsByComputerNameAsync(Environment.MachineName);

                // Fetch Order Settings and App Date ASAP for startup screens
                var settingsTask = _orderSettingsService.GetOrderSettingsAsync(Environment.MachineName);
                var appDateTask = _appDateService.GetAppDate();
                
                await Task.WhenAll(settingsTask, appDateTask);

                var orderSettings = await settingsTask;
                if (orderSettings != null)
                {
                    _commonProperties.OrderSettings = orderSettings;
                    foreach (var item in orderSettings)
                    {
                        if (item.OrderType == "DineIn")
                        {
                            _commonProperties.DineInSettings = new()
                            {
                                OrderStatment = item.OrderStatment, JobID = item.JobID, Service = item.Service, Tax = item.Tax,
                                Tips = item.Tips, SeparateReceiptCount = item.SeparateReceiptCount, AddServiceToItemPrice = item.AddServiceToItemPrice,
                                ClosingReceiptCount = item.ClosingReceiptCount, CustomerReceiptCount = item.CustomerReceiptCount, FullKitchenReceiptCount = item.FullKitchenReceiptCount,
                                CanCloseWithoutPrint = item.CanCloseWithoutPrint, DeductCaptainTips = item.DeductCaptainTips, CaptainTipsAmount = item.CaptainTipsAmount
                            };
                        }
                        else if (item.OrderType == "TakeAway")
                        {
                            _commonProperties.TakeAwaySettings = new()
                            {
                                OrderStatment = item.OrderStatment, JobID = item.JobID, Service = item.Service, Tax = item.Tax,
                                Tips = item.Tips, SeparateReceiptCount = item.SeparateReceiptCount, AddServiceToItemPrice = item.AddServiceToItemPrice,
                                ClosingReceiptCount = item.ClosingReceiptCount, CustomerReceiptCount = item.CustomerReceiptCount, FullKitchenReceiptCount = item.FullKitchenReceiptCount
                            };
                        }
                        else if (item.OrderType == "Delivery")
                        {
                            _commonProperties.DeliverySettings = new()
                            {
                                OrderStatment = item.OrderStatment, JobID = item.JobID, Service = item.Service, Tax = item.Tax,
                                Tips = item.Tips, SeparateReceiptCount = item.SeparateReceiptCount, AddServiceToItemPrice = item.AddServiceToItemPrice,
                                ClosingReceiptCount = item.ClosingReceiptCount, CustomerReceiptCount = item.CustomerReceiptCount, FullKitchenReceiptCount = item.FullKitchenReceiptCount
                            };
                        }
                    }
                }

                var appDate = await appDateTask;
                if (appDate != null)
                {
                    _commonProperties.PosDate = DateOnly.FromDateTime(appDate.PosDate);
                    _commonProperties.CurrentOrderId = appDate.CurrentOrderNumber;
                }

                string startupUrl = "/pos"; // Default fallback
                _commonProperties.CurrentPosMode = "TakeAway";

                if (_commonProperties.FeatureSettings != null)
                {
                    if (_commonProperties.FeatureSettings.FirstOrDefault(s => s.FeatureName == "Startup_Delivery")?.Value == true)
                    {
                        startupUrl = "/delivery";
                        _commonProperties.CurrentPosMode = "Delivery";
                    }
                    else if (_commonProperties.FeatureSettings.FirstOrDefault(s => s.FeatureName == "Startup_DineIn")?.Value == true)
                    {
                        startupUrl = "/dinein";
                        _commonProperties.CurrentPosMode = "DineIn";
                    }
                    else if (_commonProperties.FeatureSettings.FirstOrDefault(s => s.FeatureName == "Startup_Distribution")?.Value == true)
                    {
                        startupUrl = "/distribution";
                        _commonProperties.CurrentPosMode = "Distribution";
                    }
                    else // TakeAway or default
                    {
                        startupUrl = "/pos";
                        _commonProperties.CurrentPosMode = "TakeAway";
                    }
                }

                _commonProperties.TableItems = new List<TableItem>(); // Explicitly clear right before navigation to avoid NavLock
                _commonProperties.NotifyStateChanged();
                
                // Safe navigation for Blazor Hybrid after login
                await SafeNavigateAsync(startupUrl);
            }
            else
            {
                Snackbar.Add("فشل تسجيل الدخول: اسم المستخدم أو الرقم السري غير صحيح", Severity.Error);
                _pin = string.Empty;
                StateHasChanged(); 
                await pinInput.FocusAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("فشل تسجيل الدخول: حدث خطأ أثناء عملية الدخول", Severity.Error);
            _pin = string.Empty;
            StateHasChanged();
            await pinInput.FocusAsync();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoginAction();
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

    private async Task SafeNavigateAsync(string uri)
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 100;

        while (currentRetry < maxRetries)
        {
            try
            {
                await Task.Delay(delayMs);
                
                // Check if NavigationManager is initialized by checking the Uri property
                if (_navigationManager != null && !string.IsNullOrEmpty(_navigationManager.Uri))
                {
                    // Use InvokeAsync to ensure we're on the correct synchronization context
                    await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
                    return;
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager not yet initialized");
                }
            }
            catch (InvalidOperationException)
            {
                currentRetry++;
                
                if (currentRetry >= maxRetries)
                {
                    Snackbar.Add("Navigation error after login. Please refresh.", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }

    private void ExitApp()
    {
        Application.Current.Shutdown();
    }
}

