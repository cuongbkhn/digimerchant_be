namespace DigiMerchantBE.Entities;

public class DmRole
{
    public long RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RoleLevel { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreateTime { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdateTime { get; set; }

    public ICollection<DmUser> Users { get; set; } = new List<DmUser>();
    public ICollection<DmRoleFunc> RoleFunctions { get; set; } = new List<DmRoleFunc>();
}
