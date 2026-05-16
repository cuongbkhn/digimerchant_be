namespace DigiMerchantBE.Common;

public sealed class ApiException : Exception
{
    public ApiException(ApiErrorDefinition error, string? descriptionOverride = null)
        : base(descriptionOverride ?? error.Description)
    {
        StatusCode = error.HttpStatusCode;
        Error = error;
        ErrorCode = error.Code;
        ErrorDescription = descriptionOverride ?? error.Description;
    }

    public ApiErrorDefinition Error { get; }
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string ErrorDescription { get; }
}
