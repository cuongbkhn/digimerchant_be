using Microsoft.AspNetCore.Http;

namespace DigiMerchantBE.Options;

public class RefreshTokenCookieOptions
{
    public string Name { get; set; } = "refresh_token";
    public string Path { get; set; } = "/";
    public string? Domain { get; set; }
    public string SameSite { get; set; } = "Lax";
    public string SecurePolicy { get; set; } = "SameAsRequest";
    public bool HttpOnly { get; set; } = true;

    public SameSiteMode GetSameSiteMode()
    {
        return SameSite.ToUpperInvariant() switch
        {
            "NONE" => SameSiteMode.None,
            "STRICT" => SameSiteMode.Strict,
            _ => SameSiteMode.Lax
        };
    }

    public CookieSecurePolicy GetSecurePolicy()
    {
        return SecurePolicy.ToUpperInvariant() switch
        {
            "ALWAYS" => CookieSecurePolicy.Always,
            "NONE" => CookieSecurePolicy.None,
            _ => CookieSecurePolicy.SameAsRequest
        };
    }
}
