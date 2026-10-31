using System.ComponentModel.DataAnnotations;

namespace POS.Contract.Dtos.AccountDtos;

public class ChangeOwnPasswordDto
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(3, ErrorMessage = "New password must be at least 3 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
