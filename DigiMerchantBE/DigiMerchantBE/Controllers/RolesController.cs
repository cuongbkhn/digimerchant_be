using DigiMerchantBE.DTOs.Roles;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMerchantBE.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>Danh sách role cho dropdown đăng ký user (không có SUPER_ADMIN, theo quyền người gọi).</summary>
    [HttpGet]
    [RequireFunction("USER_MANAGEMENT")]
    public async Task<ActionResult<RoleListResponse>> GetRegisterableRoles()
    {
        var result = await _roleService.GetRegisterableRolesAsync();
        return Ok(result);
    }
}
