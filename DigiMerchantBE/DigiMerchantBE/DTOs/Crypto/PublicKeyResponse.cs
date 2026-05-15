namespace DigiMerchantBE.DTOs.Crypto;

public class PublicKeyResponse
{
    public string Kid { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PublicKeyPem { get; set; } = string.Empty;
}
