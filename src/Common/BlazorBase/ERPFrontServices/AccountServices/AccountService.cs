using POS.Contract.Dtos.AccountDtos;
using BlazorBase.API;
using BlazorBase.Helpers;
using System.Text.Json;
using System.Net.Http.Json;

namespace BlazorBase.ERPFrontServices.AccountServices;

public class AccountService : IAccountService
{
    private readonly ApiSettings _apiSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountService> _logger;

    public AccountService(ApiSettings apiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AccountService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings.ApiName!);
    }

    public async Task<IEnumerable<UserDto>> GetUsers()
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetUsers);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<List<UserDto>>(content) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
        }
        return [];
    }

    public async Task<IEnumerable<UserDto>> GetUsersWithRoles()
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetUsersWithRoles);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<List<UserDto>>(content) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with roles");
        }
        return [];
    }

    public async Task<IEnumerable<RoleToReturnDto>> GetRoles()
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllRoles);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<List<RoleToReturnDto>>(content) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
        }
        return [];
    }

    public async Task<bool> CreateUser(RegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateUser, registerDto);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                try 
                {
                    var errorResponse = ApiRequestHelpers.DeserializeResponseContent<BlazorBase.API.ApiValidationErrorResponse>(content);
                    if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                    {
                        var joinedErrors = string.Join(", ", errorResponse.Errors);
                        if (joinedErrors.Contains("This User already exists!"))
                            throw new Exception("اسم المستخدم موجود بالفعل!");
                        
                        throw new Exception(joinedErrors);
                    }
                    if (!string.IsNullOrEmpty(errorResponse?.Message))
                    {
                        throw new Exception(errorResponse.Message);
                    }
                }
                catch (JsonException) { }
                catch (Exception ex) when (ex.Message != content) { throw; }

                throw new Exception("حدث خطأ أثناء إنشاء المستخدم. يرجى التحقق من البيانات.");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw; 
        }
    }

    public async Task<bool> UpdateUser(UserDto userDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.UpdateUser, userDto);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error updating user. Status: {response.StatusCode} Content: {content}");
                
                try 
                {
                    var errorResponse = ApiRequestHelpers.DeserializeResponseContent<BlazorBase.API.ApiValidationErrorResponse>(content);
                    if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                    {
                        throw new Exception(string.Join(", ", errorResponse.Errors));
                    }
                    if (!string.IsNullOrEmpty(errorResponse?.Message))
                    {
                        throw new Exception(errorResponse.Message);
                    }
                }
                catch (JsonException) { }
                catch (Exception ex) when (ex.Message != content) { throw; }

                throw new Exception("حدث خطأ أثناء تحديث البيانات. يرجى المحاولة مرة أخرى.");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            throw; // Re-throw to be caught by UI
        }
    }

    public async Task<bool> DeleteUser(string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteUser}/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return false;
        }
    }

    public async Task<bool> CreateRole(string roleName)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_apiSettings.Endpoints!.CreateRole}?Name={Uri.EscapeDataString(roleName)}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return false;
        }
    }

    public async Task<bool> DeleteRole(string roleName)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteRole}/{Uri.EscapeDataString(roleName)}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            return false;
        }
    }
    public async Task<bool> CheckUserExists(string userName)
    {
        try
        {
            var baseUrl = _apiSettings.Endpoints!.CheckUserExists ?? "api/Account/userExists";
            var response = await _httpClient.GetAsync($"{baseUrl}/{Uri.EscapeDataString(userName)}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking user existence for {userName}");
            return false;
        }
    }
    public async Task<IEnumerable<PermissionDto>> GetAllPermissions()
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllPermissions);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<List<PermissionDto>>(content) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions");
        }
        return [];
    }

    public async Task<IEnumerable<string>> GetRolePermissions(string roleName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetRolePermissions}/{Uri.EscapeDataString(roleName)}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<List<string>>(content) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting role permissions for {roleName}");
        }
        return Enumerable.Empty<string>();
    }

    public async Task<bool> UpdateRolePermissions(string roleName, List<RolePermissionItemDto> permissions)
    {
        try
        {
            var request = new { RoleName = roleName, Permissions = permissions };
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.UpdateRolePermissions, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating role permissions for {roleName}");
            return false;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> AdminResetPassword(AdminResetPasswordDto dto)
    {
        try
        {
            var endpoint = _apiSettings.Endpoints?.AdminResetPassword ?? "/api/Account/AdminResetPassword";
            var response = await _httpClient.PostAsJsonAsync(endpoint, dto);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var errorObj = ApiRequestHelpers.DeserializeResponseContent<BlazorBase.API.ApiResponse>(content);
                if (!string.IsNullOrEmpty(errorObj?.Message))
                    return (false, errorObj.Message);
            }
            catch { }

            return (false, string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", dto.UserId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangeOwnPassword(ChangeOwnPasswordDto dto)
    {
        try
        {
            var endpoint = _apiSettings.Endpoints?.ChangeOwnPassword ?? "/api/Account/ChangeOwnPassword";
            var response = await _httpClient.PostAsJsonAsync(endpoint, dto);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var errorObj = ApiRequestHelpers.DeserializeResponseContent<BlazorBase.API.ApiResponse>(content);
                if (!string.IsNullOrEmpty(errorObj?.Message))
                    return (false, errorObj.Message);
            }
            catch { }

            return (false, string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing own password");
            return (false, ex.Message);
        }
    }
}
