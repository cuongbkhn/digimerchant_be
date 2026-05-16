namespace DigiMerchantBE.Entities;

public class DmIconConfig
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
    public int GridSpan { get; set; } = 1;
    public string Status { get; set; } = "DRAFT";
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? AppVersion { get; set; }
    public string Platform { get; set; } = "ALL";
    public bool LoginRequired { get; set; } = true;
    public string? TrackingCode { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
