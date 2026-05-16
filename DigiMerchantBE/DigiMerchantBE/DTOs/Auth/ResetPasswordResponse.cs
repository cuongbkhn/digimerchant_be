using DigiMerchantBE.Common;

namespace DigiMerchantBE.DTOs.Auth;

public class ResetPasswordResponse : IApiResult
{
    public string ErrorCode { get; set; } = ApiErrorCodes.Success.Code;
    public string ErrorDescription { get; set; } = ApiErrorCodes.Success.Description;
    public string Username { get; set; } = string.Empty;
    public string RawPassword { get; set; } = string.Empty;
}
