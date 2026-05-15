using System.ComponentModel.DataAnnotations;

namespace DigiMerchantBE.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
}
