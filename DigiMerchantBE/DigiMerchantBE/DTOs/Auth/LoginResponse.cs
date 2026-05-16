using DigiMerchantBE.Common;

namespace DigiMerchantBE.DTOs.Auth;

public class LoginResponse : IApiResult
{
    public string ErrorCode { get; set; } = ApiErrorCodes.Success.Code;
    public string ErrorDescription { get; set; } = ApiErrorCodes.Success.Description;
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public CurrentUserResponse User { get; set; } = new();
}
