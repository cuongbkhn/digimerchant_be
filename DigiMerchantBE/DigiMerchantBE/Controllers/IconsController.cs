using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Icons;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/icons")]
[Authorize]
public class IconsController : ControllerBase
{
    private readonly IIconService _iconService;
    private readonly ICryptoEnvelopeService _cryptoEnvelopeService;

    public IconsController(IIconService iconService, ICryptoEnvelopeService cryptoEnvelopeService)
    {
        _iconService = iconService;
        _cryptoEnvelopeService = cryptoEnvelopeService;
    }

    [HttpGet("function-codes")]
    [RequireFunction("ICON_MANAGEMENT")]
    public async Task<ActionResult<ApiResponse<List<IconFunctionCodeResponse>>>> GetFunctionCodes(
        [FromQuery] string environmentCode,
        [FromQuery] string? groupCode,
        [FromQuery] string? categoryCode,
        [FromQuery] string? status,
        [FromQuery] string? keyword)
    {
        var result = await _iconService.GetFunctionCodesAsync(environmentCode, groupCode, categoryCode, status, keyword);
        return Ok(Wrap(result));
    }

    [HttpGet]
    [RequireFunction("ICON_MANAGEMENT")]
    public async Task<ActionResult<ApiResponse<PagedResult<IconResponse>>>> GetIcons([FromQuery] IconQueryRequest request)
    {
        var result = await _iconService.GetIconsAsync(request);
        return Ok(Wrap(result));
    }

    [HttpGet("{iconId:long}")]
    [RequireFunction("ICON_MANAGEMENT")]
    public async Task<ActionResult<ApiResponse<IconResponse>>> GetById(long iconId)
    {
        var result = await _iconService.GetIconByIdAsync(iconId);
        return Ok(Wrap(result));
    }

    [HttpPost]
    [RequireFunction("ICON_CREATE")]
    public async Task<ActionResult<ApiResponse<IconResponse>>> Create(
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<CreateIconRequest>(requestBody, HttpContext, cancellationToken);
        var result = await _iconService.CreateIconAsync(request);
        return Ok(Wrap(result));
    }

    [HttpPut("{iconId:long}")]
    [RequireFunction("ICON_UPDATE")]
    public async Task<ActionResult<ApiResponse<IconResponse>>> Update(
        long iconId,
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<UpdateIconRequest>(requestBody, HttpContext, cancellationToken);
        var result = await _iconService.UpdateIconAsync(iconId, request);
        return Ok(Wrap(result));
    }

    [HttpDelete("{iconId:long}")]
    [RequireFunction("ICON_DELETE")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long iconId)
    {
        await _iconService.DeleteIconAsync(iconId);
        return Ok(Wrap<object?>(null));
    }

    [HttpPut("{iconId:long}/status")]
    [RequireFunction("ICON_CHANGE_STATUS")]
    public async Task<ActionResult<ApiResponse<object?>>> ChangeStatus(
        long iconId,
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<ChangeIconStatusRequest>(requestBody, HttpContext, cancellationToken);
        await _iconService.ChangeIconStatusAsync(iconId, request.Status);
        return Ok(Wrap<object?>(null));
    }

    private static ApiResponse<T> Wrap<T>(T data) => new()
    {
        ErrorCode = ApiErrorCodes.Success.Code,
        ErrorDescription = ApiErrorCodes.Success.Description,
        Data = data
    };
}
