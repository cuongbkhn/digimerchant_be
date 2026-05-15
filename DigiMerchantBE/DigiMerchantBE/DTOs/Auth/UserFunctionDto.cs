namespace DigiMerchantBE.DTOs.Auth;

public class UserFunctionDto
{
    public long FunctionId { get; set; }
    public string FunctionCode { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? FunctionUrl { get; set; }
    public int FunctionDisplay { get; set; }
}
