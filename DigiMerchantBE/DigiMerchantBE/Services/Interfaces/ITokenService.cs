using DigiMerchantBE.Entities;

namespace DigiMerchantBE.Services.Interfaces;

public interface ITokenService
{
    (string AccessToken, string JwtId, DateTime ExpiresAt) GenerateAccessToken(
        DmUser user,
        DmRole role,
        IReadOnlyCollection<DmFunction> functions);

    string GenerateRefreshToken();
    string HashToken(string token);
}
