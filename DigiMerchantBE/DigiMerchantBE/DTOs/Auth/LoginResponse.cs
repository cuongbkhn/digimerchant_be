namespace DigiMerchantBE.DTOs.Auth;

public class LoginResponse
{
    public string ErrorCode { get; set; } = "00";
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public CurrentUserResponse User { get; set; } = new();
}
