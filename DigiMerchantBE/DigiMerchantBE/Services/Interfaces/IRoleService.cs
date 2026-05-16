using DigiMerchantBE.DTOs.Roles;
using DigiMerchantBE.Entities;

namespace DigiMerchantBE.Services.Interfaces;

public interface IRoleService
{
    Task<RoleListResponse> GetRegisterableRolesAsync();
    Task EnsureCanAssignRoleAsync(DmRole actorRole, DmRole targetRole);
    Task EnsureCanResetPasswordAsync(DmRole actorRole, DmUser targetUser);
    Task<DmRole> GetActorRoleAsync();
}
