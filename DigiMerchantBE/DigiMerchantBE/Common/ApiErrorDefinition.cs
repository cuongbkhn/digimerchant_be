namespace DigiMerchantBE.Common;

public sealed class ApiErrorDefinition
{
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required int HttpStatusCode { get; init; }
}
