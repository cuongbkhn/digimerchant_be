using DigiMerchantBE.DTOs.Auth;

namespace DigiMerchantBE.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync();
    Task LogoutAsync();
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<CurrentUserResponse> GetCurrentUserAsync();
}
