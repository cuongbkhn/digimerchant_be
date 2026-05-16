using DigiMerchantBE.Common;

namespace DigiMerchantBE.DTOs.Users;

public class RegisterUserResponse : IApiResult
{
    public string ErrorCode { get; set; } = ApiErrorCodes.Success.Code;
    public string ErrorDescription { get; set; } = ApiErrorCodes.Success.Description;
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
