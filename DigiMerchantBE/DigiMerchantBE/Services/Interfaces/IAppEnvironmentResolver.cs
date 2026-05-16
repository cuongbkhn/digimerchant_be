namespace DigiMerchantBE.Services.Interfaces;

public interface IAppEnvironmentResolver
{
    string Resolve(string? environmentCode);

    Task<string> ResolveForBackofficeAsync(string? requestedEnvironmentCode, long userId);

    bool IsValid(string? environmentCode);
}
