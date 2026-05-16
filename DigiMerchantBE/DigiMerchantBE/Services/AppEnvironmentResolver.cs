using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.Options;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Services;

public class AppEnvironmentResolver : IAppEnvironmentResolver
{
    private readonly AppDbContext _dbContext;
    private readonly MobileConfigOptions _options;

    public AppEnvironmentResolver(AppDbContext dbContext, IOptions<MobileConfigOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public bool IsValid(string? environmentCode)
    {
        if (string.IsNullOrWhiteSpace(environmentCode))
        {
            return false;
        }

        return GetAllowedSet().Contains(Normalize(environmentCode));
    }

    public string Resolve(string? environmentCode)
    {
        if (!IsValid(environmentCode))
        {
            throw new ApiException(ApiErrorCodes.EnvironmentRequired);
        }

        return Normalize(environmentCode!);
    }

    public async Task<string> ResolveForBackofficeAsync(string? requestedEnvironmentCode, long userId)
    {
        var env = Resolve(requestedEnvironmentCode);
        await EnsureUserCanAccessEnvironmentAsync(userId, env);
        return env;
    }

    private async Task EnsureUserCanAccessEnvironmentAsync(long userId, string environmentCode)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == 1);

        if (user?.Role is null)
        {
            throw new ApiException(ApiErrorCodes.EnvironmentAccessDenied);
        }

        if (string.Equals(user.Role.RoleCode, RoleConstants.SuperAdminCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(environmentCode, AppEnvironments.Prod, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(
                ApiErrorCodes.EnvironmentAccessDenied,
                "Bạn không có quyền thao tác cấu hình PROD. Vui lòng liên hệ quản trị viên.");
        }

        if (string.Equals(environmentCode, AppEnvironments.Uat, StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentCode, AppEnvironments.Pilot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new ApiException(ApiErrorCodes.EnvironmentAccessDenied);
    }

    private HashSet<string> GetAllowedSet()
    {
        var allowed = _options.AllowedEnvironments is { Length: > 0 }
            ? _options.AllowedEnvironments
            : AppEnvironments.All;

        return allowed
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
