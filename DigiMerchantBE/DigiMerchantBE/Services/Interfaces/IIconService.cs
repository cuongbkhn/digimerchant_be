using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Icons;

namespace DigiMerchantBE.Services.Interfaces;

public interface IIconService
{
    Task<PagedResult<IconResponse>> GetIconsAsync(IconQueryRequest request);
    Task<IconResponse> GetIconByIdAsync(long iconId);
    Task<IconResponse> CreateIconAsync(CreateIconRequest request);
    Task<IconResponse> UpdateIconAsync(long iconId, UpdateIconRequest request);
    Task ChangeIconStatusAsync(long iconId, string status);
    Task DeleteIconAsync(long iconId);
    Task<List<IconFunctionCodeResponse>> GetFunctionCodesAsync(
        string environmentCode,
        string? groupCode,
        string? categoryCode,
        string? status,
        string? keyword);
}
