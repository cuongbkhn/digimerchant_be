using Microsoft.AspNetCore.Authorization;

namespace DigiMerchantBE.Security;

public sealed class RequireFunctionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Function:";

    public RequireFunctionAttribute(string functionCode)
    {
        Policy = $"{PolicyPrefix}{functionCode}";
    }
}
