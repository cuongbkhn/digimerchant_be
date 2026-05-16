namespace DigiMerchantBE.Entities;

public class DmBannerConfig
{
    public long BannerId { get; set; }
    public string EnvironmentCode { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string? CategoryCode { get; set; }
    public string? TypeCode { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? ButtonText { get; set; }
    public string? ActionType { get; set; }
    public string? FunctionCode { get; set; }
    public string? DeepLink { get; set; }
    public string? WebUrl { get; set; }
    public string? MobileFunctionCodes { get; set; }
    public string? AspectRatioCode { get; set; }
    public decimal? AspectRatioValue { get; set; }
    public int? ImageWidthPx { get; set; }
    public int? ImageHeightPx { get; set; }
    public string? RenderMode { get; set; }
    public int Priority { get; set; }
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
