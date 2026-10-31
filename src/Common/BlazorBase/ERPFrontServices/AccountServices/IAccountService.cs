using POS.Contract.Dtos.AccountDtos;

namespace BlazorBase.ERPFrontServices.AccountServices;

public interface IAccountService
{
    Task<IEnumerable<UserDto>> GetUsers();
    Task<IEnumerable<UserDto>> GetUsersWithRoles();
    Task<IEnumerable<RoleToReturnDto>> GetRoles();
    Task<bool> CreateUser(RegisterDto registerDto);
    Task<bool> UpdateUser(UserDto userDto);
    Task<bool> DeleteUser(string userId);
    Task<bool> CreateRole(string roleName);
    Task<bool> DeleteRole(string roleName);
    Task<bool> CheckUserExists(string userName);
    Task<IEnumerable<PermissionDto>> GetAllPermissions();
    Task<IEnumerable<string>> GetRolePermissions(string roleName);
    Task<bool> UpdateRolePermissions(string roleName, List<RolePermissionItemDto> permissions);
    Task<(bool Success, string? ErrorMessage)> AdminResetPassword(AdminResetPasswordDto dto);
    Task<(bool Success, string? ErrorMessage)> ChangeOwnPassword(ChangeOwnPasswordDto dto);
}
