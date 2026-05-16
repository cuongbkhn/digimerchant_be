using DigiMerchantBE.DTOs.MobileConfig;

namespace DigiMerchantBE.Services.Interfaces;

public interface IMobileConfigService
{
    Task<MobileBootstrapConfigResponse> GetPublicBootstrapAsync(string environmentCode, MobileConfigRequest request);

    Task<MobileBootstrapConfigResponse> GetAuthenticatedBootstrapAsync(string environmentCode, MobileConfigRequest request);
}
