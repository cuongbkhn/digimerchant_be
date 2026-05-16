namespace DigiMerchantBE.DTOs.Icons;

public class MobileIconGroupResponse
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<MobileIconResponse> Items { get; set; } = [];
}
