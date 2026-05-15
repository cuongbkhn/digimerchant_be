using DigiMerchantBE.DTOs.Crypto;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/crypto")]
public class CryptoController : ControllerBase
{
    private readonly ICryptoEnvelopeService _cryptoEnvelopeService;

    public CryptoController(ICryptoEnvelopeService cryptoEnvelopeService)
    {
        _cryptoEnvelopeService = cryptoEnvelopeService;
    }

    [HttpGet("public-key")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<PublicKeyResponse>>> GetPublicKeys([FromQuery] string? kid, CancellationToken cancellationToken)
    {
        var result = await _cryptoEnvelopeService.GetPublicKeysAsync(kid, cancellationToken);
        return Ok(result);
    }
}
