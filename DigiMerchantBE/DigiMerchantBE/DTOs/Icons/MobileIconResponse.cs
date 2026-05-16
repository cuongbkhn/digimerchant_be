namespace DigiMerchantBE.DTOs.Icons;

public class MobileIconResponse
{
    public long IconId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string? CategoryCode { get; set; }
    public string? TypeCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string? IconSelectedUrl { get; set; }
    public string? IconDisabledUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? FunctionCode { get; set; }
    public string? DeepLink { get; set; }
    public string? WebUrl { get; set; }
    public string? ActionType { get; set; }
    public MobileIconBadgeDto? Badge { get; set; }
    public int Priority { get; set; }
    public int GridSpan { get; set; }
    public string? TrackingCode { get; set; }
}
