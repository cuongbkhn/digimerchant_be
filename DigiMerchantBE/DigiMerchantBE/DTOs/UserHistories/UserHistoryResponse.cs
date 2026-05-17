namespace DigiMerchantBE.DTOs.UserHistories;

public class UserHistoryResponse
{
    public long HistoryId { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ActorFullName { get; set; }
    public string? ActorRoleCode { get; set; }
    public string? ActorRoleName { get; set; }
    public long? FunctionId { get; set; }
    public string? FuncName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActionDesc { get; set; }
    public DateTime ActionDate { get; set; }
    public string? EditTable { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
