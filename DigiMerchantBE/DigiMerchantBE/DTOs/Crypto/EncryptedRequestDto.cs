using System.ComponentModel.DataAnnotations;

namespace DigiMerchantBE.DTOs.Crypto;

public class EncryptedRequestDto
{
    [Required]
    public string Kid { get; set; } = string.Empty;

    [Required]
    public string Alg { get; set; } = string.Empty;

    [Required]
    public string K { get; set; } = string.Empty;

    [Required]
    public string D { get; set; } = string.Empty;

    public long Ts { get; set; }

    [Required]
    public string Nonce { get; set; } = string.Empty;
}
