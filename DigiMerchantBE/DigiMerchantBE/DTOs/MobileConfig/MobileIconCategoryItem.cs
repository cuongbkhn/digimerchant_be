namespace DigiMerchantBE.DTOs.MobileConfig;

/// <summary>
/// Nhóm category suy ra từ icon active (không có bảng DM_ICON_CATEGORY).
/// </summary>
public class MobileIconCategoryItem
{
    public string? GroupCode { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string? TypeCode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Priority { get; set; }
}
