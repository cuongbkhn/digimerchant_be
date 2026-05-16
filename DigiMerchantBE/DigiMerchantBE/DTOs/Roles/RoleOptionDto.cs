namespace DigiMerchantBE.DTOs.Roles;

public class RoleOptionDto
{
    public long RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RoleLevel { get; set; }
}
