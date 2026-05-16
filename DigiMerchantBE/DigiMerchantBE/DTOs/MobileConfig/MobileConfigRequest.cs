namespace DigiMerchantBE.DTOs.MobileConfig;

public class MobileConfigRequest
{
    public string EnvironmentCode { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public string? AppVersion { get; set; }
    public string? ConfigVersion { get; set; }
}
