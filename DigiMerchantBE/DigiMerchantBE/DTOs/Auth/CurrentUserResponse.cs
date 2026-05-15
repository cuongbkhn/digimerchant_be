namespace DigiMerchantBE.DTOs.Auth;

public class CurrentUserResponse
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public IReadOnlyCollection<UserFunctionDto> Functions { get; set; } = Array.Empty<UserFunctionDto>();
}
