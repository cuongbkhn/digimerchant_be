using DigiMerchantBE.Common;

namespace DigiMerchantBE.DTOs.Roles;

public class RoleListResponse : IApiResult
{
    public string ErrorCode { get; set; } = ApiErrorCodes.Success.Code;
    public string ErrorDescription { get; set; } = ApiErrorCodes.Success.Description;
    public RoleOptionDto[] Roles { get; set; } = Array.Empty<RoleOptionDto>();
}
