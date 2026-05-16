using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Roles;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public RoleService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<RoleListResponse> GetRegisterableRolesAsync()
    {
        var actorRole = await GetActorRoleAsync();

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Status == "ACTIVE"
                && r.RoleLevel > actorRole.RoleLevel
                && r.RoleCode.ToUpper() != RoleConstants.SuperAdminCode)
            .OrderBy(r => r.RoleLevel)
            .ThenBy(r => r.RoleName)
            .Select(r => new RoleOptionDto
            {
                RoleId = r.RoleId,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                Description = r.Description,
                RoleLevel = r.RoleLevel
            })
            .ToArrayAsync();

        return new RoleListResponse
        {
            Roles = roles
        }.WithSuccess();
    }

    public Task EnsureCanAssignRoleAsync(DmRole actorRole, DmRole targetRole)
    {
        EnsureSubordinateRole(actorRole, targetRole, isRegister: true);
        return Task.CompletedTask;
    }

    public Task EnsureCanResetPasswordAsync(DmRole actorRole, DmUser targetUser)
    {
        if (_currentUserService.UserId.HasValue && targetUser.UserId == _currentUserService.UserId.Value)
        {
            throw new ApiException(ApiErrorCodes.CannotResetOwnPassword);
        }

        if (targetUser.Role is null)
        {
            throw new ApiException(ApiErrorCodes.UserRoleNotAssigned);
        }

        EnsureSubordinateRole(actorRole, targetUser.Role, isRegister: false);
        return Task.CompletedTask;
    }

    public async Task<DmRole> GetActorRoleAsync()
    {
        var roleCode = _currentUserService.RoleCode;
        DmRole? role = null;

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleCode.ToUpper() == roleCode.ToUpperInvariant() && r.Status == "ACTIVE");
        }

        if (role is null && _currentUserService.UserId.HasValue)
        {
            role = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.UserId == _currentUserService.UserId.Value)
                .Select(u => u.Role)
                .FirstOrDefaultAsync();
        }

        if (role is null || !string.Equals(role.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.ActorRoleUnknown);
        }

        return role;
    }

    private static void EnsureSubordinateRole(DmRole actorRole, DmRole targetRole, bool isRegister)
    {
        if (string.Equals(targetRole.RoleCode, RoleConstants.SuperAdminCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(isRegister
                ? ApiErrorCodes.CannotCreateSuperAdmin
                : ApiErrorCodes.CannotResetSuperAdminPassword);
        }

        if (targetRole.RoleLevel <= actorRole.RoleLevel)
        {
            var actionLabel = isRegister ? "gán role" : "reset mật khẩu";
            throw new ApiException(
                ApiErrorCodes.RoleHierarchyDenied,
                ApiErrorCodes.FormatRoleHierarchyDenied(actionLabel, targetRole.RoleCode));
        }
    }
}
