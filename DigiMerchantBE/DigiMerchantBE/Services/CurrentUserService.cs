using System.Security.Claims;
using DigiMerchantBE.Services.Interfaces;

namespace DigiMerchantBE.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("nameid")
                ?? Principal?.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.FindFirstValue("username");

    public string? RoleCode => Principal?.FindFirstValue("roleCode");
}
