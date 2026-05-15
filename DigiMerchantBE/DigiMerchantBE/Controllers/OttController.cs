using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Crypto;
using DigiMerchantBE.DTOs.Ott;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [FromBody] EncryptedRequestDto encrypted,
        CancellationToken cancellationToken)
    {
        var request = await _cryptoEnvelopeService.DecryptAsync<SendOttSingleRequest>(encrypted, HttpContext, cancellationToken);

        // TODO: call OTT service with decrypted request.
        return Ok(new ApiResponse<object>
        {
            ErrorCode = "00",
            Message = "Gửi OTT thành công",
            Data = new
            {
                request.Receiver
            }
        });
    }
}
