namespace DigiMerchantBE.DTOs.UserHistories;

public class UserHistoryQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ActionType { get; set; }
    public string? EditTable { get; set; }
    public string? UserName { get; set; }
    public long? TargetUserId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
