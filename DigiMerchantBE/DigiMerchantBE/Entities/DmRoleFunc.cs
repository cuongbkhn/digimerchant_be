namespace DigiMerchantBE.Entities;

public class DmRoleFunc
{
    public long Id { get; set; }
    public long FunctionId { get; set; }
    public long RoleId { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateUser { get; set; }

    public DmFunction? Function { get; set; }
    public DmRole? Role { get; set; }
}
