using DigiMerchantBE.DTOs.Crypto;

namespace DigiMerchantBE.Services.Interfaces;

public interface ICryptoEnvelopeService
{
    Task<T> DecryptAsync<T>(
        EncryptedRequestDto request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PublicKeyResponse>> GetPublicKeysAsync(string? kid = null, CancellationToken cancellationToken = default);
}
