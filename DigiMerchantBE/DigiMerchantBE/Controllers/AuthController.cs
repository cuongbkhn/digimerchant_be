using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Auth;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken()
    {
        var result = await _authService.RefreshTokenAsync();
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> Logout()
    {
        await _authService.LogoutAsync();

        return Ok(new ApiResponse<object?>
        {
            ErrorCode = "00",
            Message = "Đăng xuất thành công",
            Data = null
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        var result = await _authService.GetCurrentUserAsync();
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [Authorize]
    [RequireFunction("USER_MANAGEMENT")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return Ok(result);
    }
}
