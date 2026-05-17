using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.UserHistories;

namespace DigiMerchantBE.Services.Interfaces;

public interface IUserHistoryQueryService
{
    Task<PagedResult<UserHistoryResponse>> GetMyHistoriesAsync(UserHistoryQueryRequest request);

    Task<PagedResult<UserHistoryResponse>> GetTeamHistoriesAsync(UserHistoryQueryRequest request);
}
