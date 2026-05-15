using System.Security.Claims;

namespace DigiMerchantBE.Services.Interfaces;

public interface ICurrentUserService
{
    ClaimsPrincipal? Principal { get; }
    long? UserId { get; }
    string? UserName { get; }
}
