using DigiMerchantBE.Common;

namespace DigiMerchantBE.Options;

public class MobileConfigOptions
{
    public string DefaultEnvironment { get; set; } = AppEnvironments.Uat;
    public string[] AllowedEnvironments { get; set; } = AppEnvironments.All;
}
