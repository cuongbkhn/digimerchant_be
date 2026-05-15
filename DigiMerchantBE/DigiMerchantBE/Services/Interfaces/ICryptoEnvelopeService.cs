using DigiMerchantBE.DTOs.Crypto;
using System.Text.Json;

namespace DigiMerchantBE.Services.Interfaces;

public interface ICryptoEnvelopeService
{
    Task<T> ResolvePayloadAsync<T>(
        JsonElement requestBody,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<T> DecryptAsync<T>(
        EncryptedRequestDto request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PublicKeyResponse>> GetPublicKeysAsync(string? kid = null, CancellationToken cancellationToken = default);
}
