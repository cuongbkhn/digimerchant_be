using DigiMerchantBE.DTOs.Users;

namespace DigiMerchantBE.Services.Interfaces;

public interface IUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request);
}
