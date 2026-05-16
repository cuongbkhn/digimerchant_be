using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Banners;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/banners")]
[Authorize]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;
    private readonly ICryptoEnvelopeService _cryptoEnvelopeService;

    public BannersController(IBannerService bannerService, ICryptoEnvelopeService cryptoEnvelopeService)
    {
        _bannerService = bannerService;
        _cryptoEnvelopeService = cryptoEnvelopeService;
    }

    [HttpGet]
    [RequireFunction("BANNER_MANAGEMENT")]
    public async Task<ActionResult<ApiResponse<PagedResult<BannerResponse>>>> GetPaged([FromQuery] BannerQueryRequest request)
    {
        var result = await _bannerService.GetPagedAsync(request);
        return Ok(Wrap(result));
    }

    [HttpDelete("{id:long}")]
    [RequireFunction("BANNER_DELETE")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id)
    {
        await _bannerService.DeleteAsync(id);
        return Ok(Wrap<object?>(null));
    }

    [HttpGet("{id:long}")]
    [RequireFunction("BANNER_MANAGEMENT")]
    public async Task<ActionResult<ApiResponse<BannerResponse>>> GetById(long id)
    {
        var result = await _bannerService.GetByIdAsync(id);
        return Ok(Wrap(result));
    }

    [HttpPost]
    [RequireFunction("BANNER_CREATE")]
    public async Task<ActionResult<ApiResponse<BannerResponse>>> Create(
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<CreateBannerRequest>(requestBody, HttpContext, cancellationToken);
        var result = await _bannerService.CreateAsync(request);
        return Ok(Wrap(result));
    }

    [HttpPut("{id:long}")]
    [RequireFunction("BANNER_UPDATE")]
    public async Task<ActionResult<ApiResponse<BannerResponse>>> Update(
        long id,
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<UpdateBannerRequest>(requestBody, HttpContext, cancellationToken);
        var result = await _bannerService.UpdateAsync(id, request);
        return Ok(Wrap(result));
    }

    [HttpPut("{id:long}/status")]
    [RequireFunction("BANNER_CHANGE_STATUS")]
    public async Task<ActionResult<ApiResponse<object?>>> ChangeStatus(
        long id,
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<ChangeBannerStatusRequest>(requestBody, HttpContext, cancellationToken);
        await _bannerService.ChangeStatusAsync(id, request.Status);
        return Ok(Wrap<object?>(null));
    }

    private static ApiResponse<T> Wrap<T>(T data) => new()
    {
        ErrorCode = ApiErrorCodes.Success.Code,
        ErrorDescription = ApiErrorCodes.Success.Description,
        Data = data
    };
}
