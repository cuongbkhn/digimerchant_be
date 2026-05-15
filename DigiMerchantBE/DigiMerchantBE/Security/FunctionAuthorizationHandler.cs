using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Security;

public sealed class FunctionRequirement : IAuthorizationRequirement
{
    public FunctionRequirement(string functionCode)
    {
        FunctionCode = functionCode;
    }

    public string FunctionCode { get; }
}

public sealed class FunctionAuthorizationHandler : AuthorizationHandler<FunctionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FunctionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var hasFunction = context.User.Claims
            .Where(c => c.Type == "function")
            .Any(c => string.Equals(c.Value, requirement.FunctionCode, StringComparison.OrdinalIgnoreCase));

        if (hasFunction)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class FunctionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public FunctionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequireFunctionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var functionCode = policyName[RequireFunctionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new FunctionRequirement(functionCode))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
