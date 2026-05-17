using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.UserHistories;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Services;

public class UserHistoryQueryService : IUserHistoryQueryService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoleService _roleService;

    public UserHistoryQueryService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IRoleService roleService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _roleService = roleService;
    }

    public Task<PagedResult<UserHistoryResponse>> GetMyHistoriesAsync(UserHistoryQueryRequest request) =>
        GetPagedAsync(request, teamScope: false);

    public Task<PagedResult<UserHistoryResponse>> GetTeamHistoriesAsync(UserHistoryQueryRequest request) =>
        GetPagedAsync(request, teamScope: true);

    private async Task<PagedResult<UserHistoryResponse>> GetPagedAsync(
        UserHistoryQueryRequest request,
        bool teamScope)
    {
        var actorUserId = RequireUserId();
        var actorRole = await _roleService.GetActorRoleAsync();
        var isSuperAdmin = IsSuperAdmin(actorRole);

        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.UserHistories.AsNoTracking();

        if (teamScope)
        {
            if (!isSuperAdmin)
            {
                query = query.Where(h =>
                    h.UserId != null
                    && _dbContext.Users.Any(u =>
                        u.UserId == h.UserId
                        && u.Status == 1
                        && u.Role != null
                        && (u.UserId == actorUserId || u.Role.RoleLevel > actorRole.RoleLevel)));
            }
        }
        else
        {
            query = query.Where(h => h.UserId == actorUserId);
        }

        if (request.TargetUserId.HasValue)
        {
            await EnsureCanViewTargetUserAsync(request.TargetUserId.Value, actorUserId, actorRole, isSuperAdmin, teamScope);
            query = query.Where(h => h.UserId == request.TargetUserId.Value);
        }

        query = ApplyFilters(query, request);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.ActionDate)
            .ThenByDescending(h => h.HistoryId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new UserHistoryResponse
            {
                HistoryId = h.HistoryId,
                UserId = h.UserId,
                UserName = h.UserName,
                ActorFullName = h.User != null ? h.User.FullName : null,
                ActorRoleCode = h.User != null && h.User.Role != null ? h.User.Role.RoleCode : null,
                ActorRoleName = h.User != null && h.User.Role != null ? h.User.Role.RoleName : null,
                FunctionId = h.FunctionId,
                FuncName = h.FuncName,
                ActionType = h.ActionType,
                ActionDesc = h.ActionDesc,
                ActionDate = h.ActionDate,
                EditTable = h.EditTable,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent
            })
            .ToListAsync();

        return new PagedResult<UserHistoryResponse>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    private static IQueryable<DmUserHistory> ApplyFilters(
        IQueryable<DmUserHistory> query,
        UserHistoryQueryRequest request)
    {
        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value;
            query = query.Where(h => h.ActionDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value;
            query = query.Where(h => h.ActionDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var actionType = request.ActionType.Trim().ToUpperInvariant();
            query = query.Where(h => h.ActionType.ToUpper() == actionType);
        }

        if (!string.IsNullOrWhiteSpace(request.EditTable))
        {
            var table = request.EditTable.Trim().ToUpperInvariant();
            query = query.Where(h => h.EditTable != null && h.EditTable.ToUpper() == table);
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var keyword = request.UserName.Trim().ToUpperInvariant();
            query = query.Where(h =>
                (h.UserName != null && h.UserName.ToUpper().Contains(keyword))
                || (h.User != null && h.User.FullName != null && h.User.FullName.ToUpper().Contains(keyword)));
        }

        return query;
    }

    private async Task EnsureCanViewTargetUserAsync(
        long targetUserId,
        long actorUserId,
        DmRole actorRole,
        bool isSuperAdmin,
        bool teamScope)
    {
        if (!teamScope && targetUserId != actorUserId)
        {
            throw new ApiException(ApiErrorCodes.HistoryAccessDenied);
        }

        if (teamScope && targetUserId == actorUserId)
        {
            return;
        }

        if (isSuperAdmin)
        {
            return;
        }

        var target = await _dbContext.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == targetUserId);

        if (target?.Role is null)
        {
            throw new ApiException(ApiErrorCodes.UserNotFound);
        }

        if (string.Equals(target.Role.RoleCode, RoleConstants.SuperAdminCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.HistoryAccessDenied);
        }

        if (target.Role.RoleLevel <= actorRole.RoleLevel)
        {
            throw new ApiException(ApiErrorCodes.HistoryAccessDenied);
        }
    }

    private long RequireUserId()
    {
        if (!_currentUserService.UserId.HasValue || _currentUserService.UserId.Value <= 0)
        {
            throw new ApiException(ApiErrorCodes.Unauthorized);
        }

        return _currentUserService.UserId.Value;
    }

    private static bool IsSuperAdmin(DmRole role) =>
        string.Equals(role.RoleCode, RoleConstants.SuperAdminCode, StringComparison.OrdinalIgnoreCase);
}
