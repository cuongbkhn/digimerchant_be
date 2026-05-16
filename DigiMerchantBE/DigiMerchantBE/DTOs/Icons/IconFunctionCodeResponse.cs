namespace DigiMerchantBE.DTOs.Icons;

public class IconFunctionCodeResponse
{
    public string FunctionCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? GroupCode { get; set; }
    public string? CategoryCode { get; set; }
    public string? TypeCode { get; set; }
}
