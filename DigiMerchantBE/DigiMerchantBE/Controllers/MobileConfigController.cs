using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.MobileConfig;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/mobile/config")]
public class MobileConfigController : ControllerBase
{
    private readonly IMobileConfigService _mobileConfigService;
    private readonly IAppEnvironmentResolver _environmentResolver;

    public MobileConfigController(
        IMobileConfigService mobileConfigService,
        IAppEnvironmentResolver environmentResolver)
    {
        _mobileConfigService = mobileConfigService;
        _environmentResolver = environmentResolver;
    }

    [HttpGet("public-bootstrap")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MobileBootstrapConfigResponse>>> GetPublicBootstrap(
        [FromQuery] MobileConfigRequest request)
    {
        var env = _environmentResolver.Resolve(request.EnvironmentCode);
        var result = await _mobileConfigService.GetPublicBootstrapAsync(env, request);
        return Ok(Wrap(result));
    }

    [HttpGet("bootstrap")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MobileBootstrapConfigResponse>>> GetBootstrap(
        [FromQuery] MobileConfigRequest request)
    {
        var env = _environmentResolver.Resolve(request.EnvironmentCode);
        var result = await _mobileConfigService.GetAuthenticatedBootstrapAsync(env, request);
        return Ok(Wrap(result));
    }

    private static ApiResponse<T> Wrap<T>(T data) => new()
    {
        ErrorCode = ApiErrorCodes.Success.Code,
        ErrorDescription = ApiErrorCodes.Success.Description,
        Data = data
    };
}
