namespace DigiMerchantBE.DTOs.Icons;

public class IconResponse
{
    public long IconId { get; set; }
    public string EnvironmentCode { get; set; } = string.Empty;
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
    public string? BadgeType { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string? BadgeBgColor { get; set; }
    public DateTime? BadgeStartTime { get; set; }
    public DateTime? BadgeEndTime { get; set; }
    public int Priority { get; set; }
    public int GridSpan { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? AppVersion { get; set; }
    public string Platform { get; set; } = string.Empty;
    public bool LoginRequired { get; set; }
    public string? TrackingCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
