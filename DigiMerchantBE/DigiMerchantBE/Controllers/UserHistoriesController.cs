using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.UserHistories;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/user-histories")]
[Authorize]
public class UserHistoriesController : ControllerBase
{
    private readonly IUserHistoryQueryService _userHistoryQueryService;

    public UserHistoriesController(IUserHistoryQueryService userHistoryQueryService)
    {
        _userHistoryQueryService = userHistoryQueryService;
    }

    /// <summary>Lịch sử thao tác của chính user đang đăng nhập.</summary>
    [HttpGet("mine")]
    [RequireFunction("USER_HISTORY_VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserHistoryResponse>>>> GetMine(
        [FromQuery] UserHistoryQueryRequest request)
    {
        var result = await _userHistoryQueryService.GetMyHistoriesAsync(request);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Lịch sử theo phân cấp: SUPER_ADMIN xem tất cả; role khác xem bản thân + user có ROLE_LEVEL thấp hơn.
    /// </summary>
    [HttpGet("team")]
    [RequireFunction("USER_HISTORY_VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserHistoryResponse>>>> GetTeam(
        [FromQuery] UserHistoryQueryRequest request)
    {
        var result = await _userHistoryQueryService.GetTeamHistoriesAsync(request);
        return Ok(Wrap(result));
    }

    private static ApiResponse<T> Wrap<T>(T data) => new()
    {
        ErrorCode = ApiErrorCodes.Success.Code,
        ErrorDescription = ApiErrorCodes.Success.Description,
        Data = data
    };
}
