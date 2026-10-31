using POS.Contract.Dtos.DineInDtos;

namespace POS.Core.Services.Contract.AccountDomainContracts;

public interface IAuthService
{
    public Task<string> CreateTokenAsync(AppUser user, IList<string> roles, List<Claim> roleClaims);

    public Task<AppUser?> CreateUserAsync(RegisterDto registerDto);

    public Task<bool> CreateRoleAsync(string roleName);

    public Task<bool> UpdateUserAsync(string userId, string newUserName, string newPassword, string newDisplayName, string newRole);
    public Task<List<AppUser>> GetAllUsersAsync();
    public Task<AppUser> GetUserAsync(string userId);
    public Task<bool> DeleteUserAsync(string userId);
    public Task<bool> DeleteRoleAsync(string roleName);

    public Task<bool> AddUserToRoleAsync(string userId, string roleName);
    public Task<bool> RemoveUserFromRoleAsync(string userId, string roleName);

    public Task<List<ApplicationRole>> GetAllRolesAsync();
    public Task<ApplicationRole> GetRoleAsync(string roleName);

    public Task<HashSet<string>> GetUserPermissionsAsync(ClaimsPrincipal user, bool? forBackOffice = null);
    public Task<HashSet<string>> GetUserPermissionsAsync(AppUser user, bool? forBackOffice = null);
    
    public Task<List<Permission>> GetAllPermissionsAsync();
    public Task<List<string>> GetRolePermissionsAsync(string roleName);


    //InUse
    public Task<List<AppUser>> GetUsersHasSpecificRole(string roleName);
}