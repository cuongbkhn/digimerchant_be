namespace DigiMerchantBE.Common;

public class ApiResponse<T> : IApiResult
{
    public string ErrorCode { get; set; } = ApiErrorCodes.Success.Code;
    public string ErrorDescription { get; set; } = ApiErrorCodes.Success.Description;
    public T? Data { get; set; }
}
