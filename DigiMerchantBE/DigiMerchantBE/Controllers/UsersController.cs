using DigiMerchantBE.DTOs.Users;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Tạo tài khoản (role lấy từ DB, không cho phép SUPER_ADMIN).</summary>
    [HttpPost("register")]
    [RequireFunction("USER_MANAGEMENT")]
    public async Task<ActionResult<RegisterUserResponse>> Register([FromBody] RegisterUserRequest request)
    {
        var result = await _userService.RegisterUserAsync(request);
        return Ok(result);
    }
}
