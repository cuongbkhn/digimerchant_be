using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Banners;

namespace DigiMerchantBE.Services.Interfaces;

public interface IBannerService
{
    Task<PagedResult<BannerResponse>> GetPagedAsync(BannerQueryRequest request);
    Task<BannerResponse> GetByIdAsync(long bannerId);
    Task<BannerResponse> CreateAsync(CreateBannerRequest request);
    Task<BannerResponse> UpdateAsync(long bannerId, UpdateBannerRequest request);
    Task ChangeStatusAsync(long bannerId, string status);
    Task DeleteAsync(long bannerId);
}
