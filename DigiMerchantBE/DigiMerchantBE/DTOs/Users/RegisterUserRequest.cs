using System.ComponentModel.DataAnnotations;

namespace DigiMerchantBE.DTOs.Users;

public class RegisterUserRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? FullName { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>ADMIN, OPERATOR, or VIEWER (theo quyền người tạo).</summary>
    [Required]
    [MaxLength(100)]
    public string RoleCode { get; set; } = string.Empty;
}
