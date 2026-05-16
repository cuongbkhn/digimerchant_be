using System.Security.Cryptography;
using System.Text.Json;
using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Icons;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Services;

public class IconService : IIconService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserHistoryService _userHistoryService;
    private readonly IAppEnvironmentResolver _environmentResolver;
    private readonly IContentCatalog _catalog;

    public IconService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IUserHistoryService userHistoryService,
        IAppEnvironmentResolver environmentResolver,
        IContentCatalog catalog)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userHistoryService = userHistoryService;
        _environmentResolver = environmentResolver;
        _catalog = catalog;
    }

    public async Task<PagedResult<IconResponse>> GetIconsAsync(IconQueryRequest request)
    {
        var userId = RequireUserId();
        var env = await _environmentResolver.ResolveForBackofficeAsync(request.EnvironmentCode, userId);
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.IconConfigs.AsNoTracking().Where(x => x.EnvironmentCode == env);

        if (!string.IsNullOrWhiteSpace(request.GroupCode))
        {
            query = query.Where(x => x.GroupCode.ToUpper() == request.GroupCode.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            query = query.Where(x => x.CategoryCode != null && x.CategoryCode.ToUpper() == request.CategoryCode.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.TypeCode))
        {
            query = query.Where(x => x.TypeCode != null && x.TypeCode.ToUpper() == request.TypeCode.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.Platform))
        {
            query = query.Where(x => x.Platform.ToUpper() == request.Platform.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status.ToUpper() == request.Status.Trim().ToUpperInvariant());
        }
        else
        {
            query = query.Where(x => x.Status != _catalog.IconDeletedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Title.ToUpper().Contains(keyword) ||
                (x.TrackingCode != null && x.TrackingCode.ToUpper().Contains(keyword)) ||
                (x.FunctionCode != null && x.FunctionCode.ToUpper().Contains(keyword)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<IconResponse>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = total,
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public async Task<IconResponse> GetIconByIdAsync(long iconId)
    {
        var icon = await FindIconOrThrowAsync(iconId, track: false);
        await EnsureBackofficeEnvironmentAccessAsync(icon.EnvironmentCode);
        return MapToResponse(icon);
    }

    public async Task<IconResponse> CreateIconAsync(CreateIconRequest request)
    {
        var environmentCode = await ValidateIconRequestAsync(request);
        var entity = MapRequestToEntity(request, environmentCode);
        entity.TrackingCode = await GenerateUniqueTrackingCodeAsync(environmentCode);
        entity.CreatedBy = _currentUserService.UserName;
        entity.CreatedAt = DateTime.UtcNow;

        _dbContext.IconConfigs.Add(entity);
        await _dbContext.SaveChangesAsync();

        await WriteIconHistoryAsync("ICON_CREATE",
            $"Tạo icon {entity.IconId} cho môi trường {environmentCode}", null, SerializeIcon(entity));

        return MapToResponse(entity);
    }

    public async Task<IconResponse> UpdateIconAsync(long iconId, UpdateIconRequest request)
    {
        var environmentCode = await ValidateIconRequestAsync(request);
        var entity = await FindIconOrThrowAsync(iconId, track: true);
        EnsureEnvironmentUnchanged(entity.EnvironmentCode, environmentCode);
        var oldJson = SerializeIcon(entity);

        ApplyRequestToEntity(entity, request);
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await WriteIconHistoryAsync("ICON_UPDATE",
            $"Cập nhật icon {entity.IconId} cho môi trường {entity.EnvironmentCode}", oldJson, SerializeIcon(entity));

        return MapToResponse(entity);
    }

    public async Task ChangeIconStatusAsync(long iconId, string status)
    {
        if (string.IsNullOrWhiteSpace(status) || !_catalog.IsIconChangeStatus(status))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "STATUS không hợp lệ");
        }

        var entity = await FindIconOrThrowAsync(iconId, track: true);
        await EnsureBackofficeEnvironmentAccessAsync(entity.EnvironmentCode);
        var oldJson = SerializeIcon(entity);
        entity.Status = status.Trim().ToUpperInvariant();
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await WriteIconHistoryAsync("ICON_CHANGE_STATUS",
            $"Đổi trạng thái icon {entity.IconId} ({entity.EnvironmentCode}) -> {entity.Status}",
            oldJson, SerializeIcon(entity));
    }

    public async Task DeleteIconAsync(long iconId)
    {
        var entity = await _dbContext.IconConfigs.FirstOrDefaultAsync(x => x.IconId == iconId);
        if (entity is null)
        {
            throw new ApiException(ApiErrorCodes.IconNotFound);
        }

        await EnsureBackofficeEnvironmentAccessAsync(entity.EnvironmentCode);

        if (entity.Status == _catalog.IconDeletedStatus)
        {
            return;
        }

        var oldJson = SerializeIcon(entity);
        entity.Status = _catalog.IconDeletedStatus;
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await WriteIconHistoryAsync("ICON_DELETE",
            $"Xóa icon {entity.IconId} ({entity.EnvironmentCode})", oldJson, SerializeIcon(entity));
    }

    public async Task<List<IconFunctionCodeResponse>> GetFunctionCodesAsync(
        string environmentCode,
        string? groupCode,
        string? categoryCode,
        string? status,
        string? keyword)
    {
        var userId = RequireUserId();
        var env = await _environmentResolver.ResolveForBackofficeAsync(environmentCode, userId);
        var statusFilter = _catalog.ResolveFunctionCodeFilterStatus(status);

        var query = _dbContext.IconConfigs.AsNoTracking()
            .Where(x => x.EnvironmentCode == env)
            .Where(x => x.FunctionCode != null && x.FunctionCode != "")
            .Where(x => x.Status.ToUpper() == statusFilter);

        if (!string.IsNullOrWhiteSpace(groupCode))
        {
            var group = groupCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.GroupCode.ToUpper() == group);
        }

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var category = categoryCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.CategoryCode != null && x.CategoryCode.ToUpper() == category);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.FunctionCode!.ToUpper().Contains(kw) ||
                x.Title.ToUpper().Contains(kw));
        }

        var icons = await query
            .OrderBy(x => x.FunctionCode)
            .ThenBy(x => x.Priority)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return icons
            .GroupBy(x => x.FunctionCode!.ToUpperInvariant())
            .Select(g => g.First())
            .OrderBy(x => x.FunctionCode)
            .Select(x => new IconFunctionCodeResponse
            {
                FunctionCode = x.FunctionCode!,
                DisplayName = x.Title,
                GroupCode = x.GroupCode,
                CategoryCode = x.CategoryCode,
                TypeCode = x.TypeCode
            })
            .ToList();
    }

    private async Task<string> ValidateIconRequestAsync(CreateIconRequest request)
    {
        var userId = RequireUserId();
        var environmentCode = await _environmentResolver.ResolveForBackofficeAsync(request.EnvironmentCode, userId);

        if (string.IsNullOrWhiteSpace(request.GroupCode))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "GROUP_CODE bắt buộc");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "TITLE bắt buộc");
        }

        if (string.IsNullOrWhiteSpace(request.IconUrl))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "ICON_URL bắt buộc");
        }

        var status = request.Status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!_catalog.IsIconWritableStatus(status))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "STATUS không hợp lệ");
        }

        request.Status = status;

        var platform = request.Platform?.Trim().ToUpperInvariant() ?? AppEnvironments.PlatformAll;
        if (!_catalog.IsPlatform(platform))
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "PLATFORM không hợp lệ");
        }

        request.Platform = platform;

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var actionType = request.ActionType.Trim().ToUpperInvariant();
            if (!_catalog.IsActionType(actionType))
            {
                throw new ApiException(ApiErrorCodes.IconInvalid, "ACTION_TYPE không hợp lệ");
            }

            request.ActionType = actionType;
        }

        if (!string.IsNullOrWhiteSpace(request.FunctionCode))
        {
            request.FunctionCode = request.FunctionCode.Trim().ToUpperInvariant();
        }

        if (request.Priority < 0)
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "PRIORITY phải >= 0");
        }

        if (request.GridSpan < 1)
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "GRID_SPAN phải >= 1");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "START_TIME phải nhỏ hơn END_TIME");
        }

        if (request.BadgeStartTime.HasValue && request.BadgeEndTime.HasValue && request.BadgeStartTime >= request.BadgeEndTime)
        {
            throw new ApiException(ApiErrorCodes.IconInvalid, "BADGE_START_TIME phải nhỏ hơn BADGE_END_TIME");
        }

        return environmentCode;
    }

    private static DmIconConfig MapRequestToEntity(CreateIconRequest request, string environmentCode) =>
        new()
        {
            EnvironmentCode = environmentCode,
            GroupCode = request.GroupCode.Trim(),
            CategoryCode = TrimOrNull(request.CategoryCode),
            TypeCode = TrimOrNull(request.TypeCode),
            Title = request.Title.Trim(),
            Subtitle = TrimOrNull(request.Subtitle),
            IconUrl = request.IconUrl.Trim(),
            IconSelectedUrl = TrimOrNull(request.IconSelectedUrl),
            IconDisabledUrl = TrimOrNull(request.IconDisabledUrl),
            BackgroundColor = TrimOrNull(request.BackgroundColor),
            TextColor = TrimOrNull(request.TextColor),
            FunctionCode = TrimOrNull(request.FunctionCode),
            DeepLink = TrimOrNull(request.DeepLink),
            WebUrl = TrimOrNull(request.WebUrl),
            ActionType = TrimOrNull(request.ActionType),
            BadgeType = TrimOrNull(request.BadgeType),
            BadgeText = TrimOrNull(request.BadgeText),
            BadgeColor = TrimOrNull(request.BadgeColor),
            BadgeBgColor = TrimOrNull(request.BadgeBgColor),
            BadgeStartTime = request.BadgeStartTime,
            BadgeEndTime = request.BadgeEndTime,
            Priority = request.Priority,
            GridSpan = request.GridSpan,
            Status = request.Status,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            AppVersion = TrimOrNull(request.AppVersion),
            Platform = request.Platform,
            LoginRequired = request.LoginRequired
        };

    private static void ApplyRequestToEntity(DmIconConfig entity, CreateIconRequest request)
    {
        entity.GroupCode = request.GroupCode.Trim();
        entity.CategoryCode = TrimOrNull(request.CategoryCode);
        entity.TypeCode = TrimOrNull(request.TypeCode);
        entity.Title = request.Title.Trim();
        entity.Subtitle = TrimOrNull(request.Subtitle);
        entity.IconUrl = request.IconUrl.Trim();
        entity.IconSelectedUrl = TrimOrNull(request.IconSelectedUrl);
        entity.IconDisabledUrl = TrimOrNull(request.IconDisabledUrl);
        entity.BackgroundColor = TrimOrNull(request.BackgroundColor);
        entity.TextColor = TrimOrNull(request.TextColor);
        entity.FunctionCode = TrimOrNull(request.FunctionCode);
        entity.DeepLink = TrimOrNull(request.DeepLink);
        entity.WebUrl = TrimOrNull(request.WebUrl);
        entity.ActionType = TrimOrNull(request.ActionType);
        entity.BadgeType = TrimOrNull(request.BadgeType);
        entity.BadgeText = TrimOrNull(request.BadgeText);
        entity.BadgeColor = TrimOrNull(request.BadgeColor);
        entity.BadgeBgColor = TrimOrNull(request.BadgeBgColor);
        entity.BadgeStartTime = request.BadgeStartTime;
        entity.BadgeEndTime = request.BadgeEndTime;
        entity.Priority = request.Priority;
        entity.GridSpan = request.GridSpan;
        entity.Status = request.Status;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.AppVersion = TrimOrNull(request.AppVersion);
        entity.Platform = request.Platform;
        entity.LoginRequired = request.LoginRequired;
    }

    private async Task<DmIconConfig> FindIconOrThrowAsync(long iconId, bool track)
    {
        var query = track ? _dbContext.IconConfigs : _dbContext.IconConfigs.AsNoTracking();
        var entity = await query.FirstOrDefaultAsync(x => x.IconId == iconId);
        if (entity is null || entity.Status == _catalog.IconDeletedStatus)
        {
            throw new ApiException(ApiErrorCodes.IconNotFound);
        }

        return entity;
    }

    private long RequireUserId()
    {
        if (!_currentUserService.UserId.HasValue || _currentUserService.UserId.Value <= 0)
        {
            throw new ApiException(ApiErrorCodes.Unauthorized);
        }

        return _currentUserService.UserId.Value;
    }

    private Task EnsureBackofficeEnvironmentAccessAsync(string environmentCode) =>
        _environmentResolver.ResolveForBackofficeAsync(environmentCode, RequireUserId());

    private static void EnsureEnvironmentUnchanged(string currentEnvironmentCode, string requestedEnvironmentCode)
    {
        if (!string.Equals(currentEnvironmentCode, requestedEnvironmentCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.EnvironmentChangeNotAllowed);
        }
    }

    private async Task<string> GenerateUniqueTrackingCodeAsync(string environmentCode)
    {
        var env = environmentCode.Trim().ToUpperInvariant();
        for (var i = 0; i < 5; i++)
        {
            var code = $"ICO_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}";
            var exists = await _dbContext.IconConfigs.AnyAsync(x =>
                x.EnvironmentCode == env && x.TrackingCode == code);
            if (!exists)
            {
                return code;
            }
        }

        return $"ICO_{Guid.NewGuid():N}"[..100];
    }

    private async Task WriteIconHistoryAsync(string actionType, string desc, string? oldValue, string? newValue) =>
        await _userHistoryService.WriteAsync(
            _currentUserService.UserId,
            _currentUserService.UserName,
            actionType,
            desc,
            "DM_ICON_CONFIG",
            oldValue,
            newValue,
            funcName: "ICON_MANAGEMENT");

    private static string SerializeIcon(DmIconConfig entity) =>
        JsonSerializer.Serialize(new
        {
            entity.IconId,
            entity.EnvironmentCode,
            entity.GroupCode,
            entity.CategoryCode,
            entity.TypeCode,
            entity.Status,
            entity.Priority,
            entity.TrackingCode,
            entity.FunctionCode
        }, JsonOptions);

    private static IconResponse MapToResponse(DmIconConfig entity) =>
        new()
        {
            IconId = entity.IconId,
            EnvironmentCode = entity.EnvironmentCode,
            GroupCode = entity.GroupCode,
            CategoryCode = entity.CategoryCode,
            TypeCode = entity.TypeCode,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            IconUrl = entity.IconUrl,
            IconSelectedUrl = entity.IconSelectedUrl,
            IconDisabledUrl = entity.IconDisabledUrl,
            BackgroundColor = entity.BackgroundColor,
            TextColor = entity.TextColor,
            FunctionCode = entity.FunctionCode,
            DeepLink = entity.DeepLink,
            WebUrl = entity.WebUrl,
            ActionType = entity.ActionType,
            BadgeType = entity.BadgeType,
            BadgeText = entity.BadgeText,
            BadgeColor = entity.BadgeColor,
            BadgeBgColor = entity.BadgeBgColor,
            BadgeStartTime = entity.BadgeStartTime,
            BadgeEndTime = entity.BadgeEndTime,
            Priority = entity.Priority,
            GridSpan = entity.GridSpan,
            Status = entity.Status,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            AppVersion = entity.AppVersion,
            Platform = entity.Platform,
            LoginRequired = entity.LoginRequired,
            TrackingCode = entity.TrackingCode,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
