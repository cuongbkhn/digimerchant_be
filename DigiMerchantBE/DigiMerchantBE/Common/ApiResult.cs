namespace DigiMerchantBE.Common;

public static class ApiResult
{
    public static void ApplySuccess(IApiResult result, ApiErrorDefinition? successDefinition = null)
    {
        var success = successDefinition ?? ApiErrorCodes.Success;
        result.ErrorCode = success.Code;
        result.ErrorDescription = success.Description;
    }

    public static T WithSuccess<T>(this T result, ApiErrorDefinition? successDefinition = null)
        where T : IApiResult
    {
        ApplySuccess(result, successDefinition);
        return result;
    }
}
