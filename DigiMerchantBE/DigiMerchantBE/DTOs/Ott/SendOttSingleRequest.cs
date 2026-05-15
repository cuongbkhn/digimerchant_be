using System.ComponentModel.DataAnnotations;

namespace DigiMerchantBE.DTOs.Ott;

public class SendOttSingleRequest
{
    [Required]
    public string Receiver { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
