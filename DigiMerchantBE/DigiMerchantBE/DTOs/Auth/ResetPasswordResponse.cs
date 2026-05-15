namespace DigiMerchantBE.DTOs.Auth;

public class ResetPasswordResponse
{
    public string ErrorCode { get; set; } = "00";
    public string Username { get; set; } = string.Empty;
    public string RawPassword { get; set; } = string.Empty;
}
