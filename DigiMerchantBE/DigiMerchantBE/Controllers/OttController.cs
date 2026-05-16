using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Ott;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/ott")]
[Authorize]
public class OttController : ControllerBase
{
    private readonly ICryptoEnvelopeService _cryptoEnvelopeService;

    public OttController(ICryptoEnvelopeService cryptoEnvelopeService)
    {
        _cryptoEnvelopeService = cryptoEnvelopeService;
    }

    [HttpPost("send-single")]
    [RequireFunction("OTT_SEND_SINGLE")]
    public async Task<ActionResult<ApiResponse<object>>> SendSingle(
        [FromBody] JsonElement requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.ResolvePayloadAsync<SendOttSingleRequest>(requestBody, HttpContext, cancellationToken);

        // TODO: call OTT service with decrypted request.
        return Ok(new ApiResponse<object>
        {
            ErrorCode = ApiErrorCodes.OttSendSuccess.Code,
            ErrorDescription = ApiErrorCodes.OttSendSuccess.Description,
            Data = new
            {
                request.Receiver
            }
        });
    }
}
