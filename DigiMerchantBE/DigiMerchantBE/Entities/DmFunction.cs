namespace DigiMerchantBE.Entities;

public class DmFunction
{
    public long FunctionId { get; set; }
    public string FunctionCode { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public int FunctionLevel { get; set; }
    public string? FunctionUrl { get; set; }
    public int FunctionOrder { get; set; }
    public long? ParentId { get; set; }
    public int FunctionDisplay { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreateTime { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdateTime { get; set; }

    public DmFunction? Parent { get; set; }
    public ICollection<DmFunction> Children { get; set; } = new List<DmFunction>();
    public ICollection<DmRoleFunc> RoleFunctions { get; set; } = new List<DmRoleFunc>();
    public ICollection<DmUserHistory> UserHistories { get; set; } = new List<DmUserHistory>();
}
