namespace DigiMerchantBE.DTOs.Banners;

public class BannerQueryRequest
{
    public string EnvironmentCode { get; set; } = string.Empty;
    public string? GroupCode { get; set; }
    public string? CategoryCode { get; set; }
    public string? TypeCode { get; set; }
    public string? Platform { get; set; }
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
