using DigiMerchantBE.DTOs.Banners;
using DigiMerchantBE.DTOs.Icons;

namespace DigiMerchantBE.DTOs.MobileConfig;

public class MobileBootstrapConfigResponse
{
    public string EnvironmentCode { get; set; } = string.Empty;
    public string ConfigVersion { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
    public List<MobileBannerResponse> Banners { get; set; } = [];
    public List<MobileIconCategoryItem> IconCategories { get; set; } = [];
    public List<MobileIconResponse> Icons { get; set; } = [];
}
