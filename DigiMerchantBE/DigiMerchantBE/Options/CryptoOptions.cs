namespace DigiMerchantBE.Options;

public class CryptoOptions
{
    public int ReplayWindowSeconds { get; set; } = 300;
    public List<RsaKeyOptions> RsaKeys { get; set; } = new();
}

public class RsaKeyOptions
{
    public string Kid { get; set; } = string.Empty;
    public string PrivateKeyPemPath { get; set; } = string.Empty;
    public string PublicKeyPemPath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
