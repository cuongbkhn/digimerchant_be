using System.Security.Cryptography;
using System.Text.Json;
using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Banners;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Services;

public class BannerService : IBannerService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserHistoryService _userHistoryService;
    private readonly IAppEnvironmentResolver _environmentResolver;
    private readonly IContentCatalog _catalog;

    public BannerService(
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

    public async Task<PagedResult<BannerResponse>> GetPagedAsync(BannerQueryRequest request)
    {
        var userId = RequireUserId();
        var environmentCode = await _environmentResolver.ResolveForBackofficeAsync(request.EnvironmentCode, userId);
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.BannerConfigs.AsNoTracking()
            .Where(x => x.EnvironmentCode == environmentCode);

        if (!string.IsNullOrWhiteSpace(request.GroupCode))
        {
            var group = request.GroupCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.GroupCode.ToUpper() == group);
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            var category = request.CategoryCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.CategoryCode != null && x.CategoryCode.ToUpper() == category);
        }

        if (!string.IsNullOrWhiteSpace(request.TypeCode))
        {
            var type = request.TypeCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.TypeCode != null && x.TypeCode.ToUpper() == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Platform))
        {
            var platform = request.Platform.Trim().ToUpperInvariant();
            query = query.Where(x => x.Platform == AppEnvironments.PlatformAll || x.Platform.ToUpper() == platform);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status.ToUpper() == status);
        }
        else
        {
            query = query.Where(x => x.Status != _catalog.BannerDeletedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToUpperInvariant();
            query = query.Where(x =>
                (x.Title != null && x.Title.ToUpper().Contains(keyword)) ||
                (x.TrackingCode != null && x.TrackingCode.ToUpper().Contains(keyword)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BannerResponse>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = total,
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public async Task<BannerResponse> GetByIdAsync(long bannerId)
    {
        var banner = await FindBannerOrThrowAsync(bannerId, track: false);
        await EnsureBackofficeEnvironmentAccessAsync(banner.EnvironmentCode);
        return MapToResponse(banner);
    }

    public async Task<BannerResponse> CreateAsync(CreateBannerRequest request)
    {
        var environmentCode = await ValidateBannerRequestAsync(request);
        var now = DateTime.UtcNow;
        var actor = _currentUserService.UserName;

        var entity = new DmBannerConfig
        {
            EnvironmentCode = environmentCode,
            GroupCode = request.GroupCode.Trim(),
            CategoryCode = TrimOrNull(request.CategoryCode),
            TypeCode = TrimOrNull(request.TypeCode),
            Title = TrimOrNull(request.Title),
            Body = TrimOrNull(request.Body),
            ImageUrl = TrimOrNull(request.ImageUrl),
            ButtonText = TrimOrNull(request.ButtonText),
            ActionType = TrimOrNull(request.ActionType),
            FunctionCode = TrimOrNull(request.FunctionCode),
            DeepLink = TrimOrNull(request.DeepLink),
            WebUrl = TrimOrNull(request.WebUrl),
            MobileFunctionCodes = MobileFunctionCodesHelper.Serialize(request.MobileFunctionCodes),
            AspectRatioCode = TrimOrNull(request.AspectRatioCode),
            AspectRatioValue = request.AspectRatioValue,
            ImageWidthPx = request.ImageWidthPx,
            ImageHeightPx = request.ImageHeightPx,
            RenderMode = TrimOrNull(request.RenderMode),
            Priority = request.Priority,
            Status = request.Status,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            AppVersion = TrimOrNull(request.AppVersion),
            Platform = request.Platform,
            LoginRequired = request.LoginRequired,
            CreatedBy = actor,
            CreatedAt = now
        };

        ApplyAspectRatioRules(entity);
        entity.TrackingCode = await GenerateUniqueTrackingCodeAsync(environmentCode);

        _dbContext.BannerConfigs.Add(entity);
        await _dbContext.SaveChangesAsync();

        await WriteHistoryAsync("BANNER_CREATE", $"Tạo banner {entity.BannerId} cho môi trường {environmentCode}", null, SerializeBanner(entity));

        return MapToResponse(entity);
    }

    public async Task<BannerResponse> UpdateAsync(long bannerId, UpdateBannerRequest request)
    {
        var environmentCode = await ValidateBannerRequestAsync(request);
        var entity = await FindBannerOrThrowAsync(bannerId, track: true);
        EnsureNotDeleted(entity);
        EnsureEnvironmentUnchanged(entity.EnvironmentCode, environmentCode);
        await EnsureBackofficeEnvironmentAccessAsync(entity.EnvironmentCode);
        var oldJson = SerializeBanner(entity);

        entity.GroupCode = request.GroupCode.Trim();
        entity.CategoryCode = TrimOrNull(request.CategoryCode);
        entity.TypeCode = TrimOrNull(request.TypeCode);
        entity.Title = TrimOrNull(request.Title);
        entity.Body = TrimOrNull(request.Body);
        entity.ImageUrl = TrimOrNull(request.ImageUrl);
        entity.ButtonText = TrimOrNull(request.ButtonText);
        entity.ActionType = TrimOrNull(request.ActionType);
        entity.FunctionCode = TrimOrNull(request.FunctionCode);
        entity.DeepLink = TrimOrNull(request.DeepLink);
        entity.WebUrl = TrimOrNull(request.WebUrl);
        entity.MobileFunctionCodes = MobileFunctionCodesHelper.Serialize(request.MobileFunctionCodes);
        entity.AspectRatioCode = TrimOrNull(request.AspectRatioCode);
        entity.AspectRatioValue = request.AspectRatioValue;
        entity.ImageWidthPx = request.ImageWidthPx;
        entity.ImageHeightPx = request.ImageHeightPx;
        entity.RenderMode = TrimOrNull(request.RenderMode);
        entity.Priority = request.Priority;
        entity.Status = request.Status;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.AppVersion = TrimOrNull(request.AppVersion);
        entity.Platform = request.Platform;
        entity.LoginRequired = request.LoginRequired;
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;

        ApplyAspectRatioRules(entity);

        await _dbContext.SaveChangesAsync();
        await WriteHistoryAsync("BANNER_UPDATE", $"Cập nhật banner {entity.BannerId} cho môi trường {entity.EnvironmentCode}", oldJson, SerializeBanner(entity));

        return MapToResponse(entity);
    }

    public async Task ChangeStatusAsync(long bannerId, string status)
    {
        if (string.IsNullOrWhiteSpace(status) || !_catalog.IsBannerChangeStatus(status))
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "STATUS không hợp lệ");
        }

        var entity = await FindBannerOrThrowAsync(bannerId, track: true);
        EnsureNotDeleted(entity);
        await EnsureBackofficeEnvironmentAccessAsync(entity.EnvironmentCode);
        var oldJson = SerializeBanner(entity);
        entity.Status = status.Trim().ToUpperInvariant();
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await WriteHistoryAsync(
            "BANNER_CHANGE_STATUS",
            $"Đổi trạng thái banner {entity.BannerId} ({entity.EnvironmentCode}) -> {entity.Status}",
            oldJson,
            SerializeBanner(entity));
    }

    public async Task DeleteAsync(long bannerId)
    {
        var entity = await FindBannerOrThrowAsync(bannerId, track: true);
        await EnsureBackofficeEnvironmentAccessAsync(entity.EnvironmentCode);

        if (string.Equals(entity.Status, _catalog.BannerDeletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var oldJson = SerializeBanner(entity);
        entity.Status = _catalog.BannerDeletedStatus;
        entity.UpdatedBy = _currentUserService.UserName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await WriteHistoryAsync(
            "BANNER_DELETE",
            $"Xóa mềm banner {entity.BannerId} ({entity.EnvironmentCode})",
            oldJson,
            SerializeBanner(entity));
    }

    private async Task<DmBannerConfig> FindBannerOrThrowAsync(long id, bool track)
    {
        var query = track ? _dbContext.BannerConfigs.AsQueryable() : _dbContext.BannerConfigs.AsNoTracking();
        var banner = await query.FirstOrDefaultAsync(x => x.BannerId == id);
        if (banner is null)
        {
            throw new ApiException(ApiErrorCodes.BannerNotFound);
        }

        return banner;
    }

    private void EnsureNotDeleted(DmBannerConfig entity)
    {
        if (string.Equals(entity.Status, _catalog.BannerDeletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.BannerNotFound);
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

    private Task EnsureBackofficeEnvironmentAccessAsync(string environmentCode) =>
        _environmentResolver.ResolveForBackofficeAsync(environmentCode, RequireUserId());

    private async Task<string> ValidateBannerRequestAsync(CreateBannerRequest request)
    {
        var userId = RequireUserId();
        var environmentCode = await _environmentResolver.ResolveForBackofficeAsync(request.EnvironmentCode, userId);

        if (string.IsNullOrWhiteSpace(request.GroupCode))
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "GROUP_CODE bắt buộc");
        }

        var status = request.Status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!_catalog.IsBannerWritableStatus(status))
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "STATUS không hợp lệ");
        }

        request.Status = status;

        var platform = request.Platform?.Trim().ToUpperInvariant() ?? AppEnvironments.PlatformAll;
        if (!_catalog.IsPlatform(platform))
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "PLATFORM không hợp lệ");
        }

        request.Platform = platform;

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var actionType = request.ActionType.Trim().ToUpperInvariant();
            if (!_catalog.IsActionType(actionType))
            {
                throw new ApiException(ApiErrorCodes.BannerInvalid, "ACTION_TYPE không hợp lệ");
            }

            request.ActionType = actionType;
        }

        if (!string.IsNullOrWhiteSpace(request.RenderMode))
        {
            var renderMode = request.RenderMode.Trim().ToUpperInvariant();
            if (!_catalog.IsBannerRenderMode(renderMode))
            {
                throw new ApiException(ApiErrorCodes.BannerInvalid, "RENDER_MODE không hợp lệ");
            }

            request.RenderMode = renderMode;
        }

        if (request.Priority < 0)
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "PRIORITY phải >= 0");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "START_TIME phải nhỏ hơn END_TIME");
        }

        if (request.ImageWidthPx.HasValue && request.ImageWidthPx <= 0)
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "IMAGE_WIDTH_PX phải lớn hơn 0");
        }

        if (request.ImageHeightPx.HasValue && request.ImageHeightPx <= 0)
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "IMAGE_HEIGHT_PX phải lớn hơn 0");
        }

        if (request.AspectRatioValue.HasValue && request.AspectRatioValue <= 0)
        {
            throw new ApiException(ApiErrorCodes.BannerInvalid, "ASPECT_RATIO_VALUE phải lớn hơn 0");
        }

        await ValidateMobileFunctionCodesAsync(environmentCode, request.MobileFunctionCodes);

        return environmentCode;
    }

    private async Task ValidateMobileFunctionCodesAsync(string environmentCode, List<string>? codes)
    {
        if (codes is null || codes.Count == 0)
        {
            return;
        }

        var normalized = codes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            return;
        }

        var allowed = await _dbContext.IconConfigs.AsNoTracking()
            .Where(x => x.EnvironmentCode == environmentCode)
            .Where(x => x.Status == "ACTIVE")
            .Where(x => x.FunctionCode != null && x.FunctionCode != "")
            .Select(x => x.FunctionCode!.ToUpper())
            .Distinct()
            .ToListAsync();

        var allowedSet = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var code in normalized)
        {
            if (!allowedSet.Contains(code))
            {
                throw new ApiException(ApiErrorCodes.BannerInvalid, $"MOBILE_FUNCTION_CODE '{code}' không hợp lệ");
            }
        }
    }

    private static void EnsureEnvironmentUnchanged(string currentEnvironmentCode, string requestedEnvironmentCode)
    {
        if (!string.Equals(currentEnvironmentCode, requestedEnvironmentCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.EnvironmentChangeNotAllowed);
        }
    }

    private void ApplyAspectRatioRules(DmBannerConfig entity)
    {
        if (entity.ImageWidthPx.HasValue && entity.ImageHeightPx.HasValue && !entity.AspectRatioValue.HasValue)
        {
            entity.AspectRatioValue = Math.Round((decimal)entity.ImageWidthPx.Value / entity.ImageHeightPx.Value, 4);
        }

        if (!string.IsNullOrWhiteSpace(entity.AspectRatioCode)
            && _catalog.TryGetBannerAspectRatio(entity.AspectRatioCode, out var ratio)
            && !entity.AspectRatioValue.HasValue)
        {
            entity.AspectRatioValue = ratio;
        }
    }

    private async Task<string> GenerateUniqueTrackingCodeAsync(string environmentCode)
    {
        var env = environmentCode.Trim().ToUpperInvariant();
        for (var i = 0; i < 5; i++)
        {
            var code = $"BNR_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}";
            var exists = await _dbContext.BannerConfigs.AnyAsync(x =>
                x.EnvironmentCode == env && x.TrackingCode == code);
            if (!exists)
            {
                return code;
            }
        }

        return $"BNR_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..100];
    }

    private async Task WriteHistoryAsync(string actionType, string desc, string? oldValue, string? newValue)
    {
        await _userHistoryService.WriteAsync(
            _currentUserService.UserId,
            _currentUserService.UserName,
            actionType,
            desc,
            "DM_BANNER_CONFIG",
            oldValue,
            newValue,
            funcName: "BANNER_MANAGEMENT");
    }

    private static string SerializeBanner(DmBannerConfig entity) =>
        JsonSerializer.Serialize(new
        {
            entity.BannerId,
            entity.EnvironmentCode,
            entity.GroupCode,
            entity.CategoryCode,
            entity.TypeCode,
            entity.Status,
            entity.Priority,
            entity.TrackingCode,
            entity.ActionType,
            entity.Platform,
            MobileFunctionCodes = MobileFunctionCodesHelper.Deserialize(entity.MobileFunctionCodes)
        }, JsonOptions);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BannerResponse MapToResponse(DmBannerConfig entity) =>
        new()
        {
            BannerId = entity.BannerId,
            EnvironmentCode = entity.EnvironmentCode,
            GroupCode = entity.GroupCode,
            CategoryCode = entity.CategoryCode,
            TypeCode = entity.TypeCode,
            Title = entity.Title,
            Body = entity.Body,
            ImageUrl = entity.ImageUrl,
            ButtonText = entity.ButtonText,
            ActionType = entity.ActionType,
            FunctionCode = entity.FunctionCode,
            DeepLink = entity.DeepLink,
            WebUrl = entity.WebUrl,
            MobileFunctionCodes = MobileFunctionCodesHelper.Deserialize(entity.MobileFunctionCodes),
            AspectRatioCode = entity.AspectRatioCode,
            AspectRatioValue = entity.AspectRatioValue,
            ImageWidthPx = entity.ImageWidthPx,
            ImageHeightPx = entity.ImageHeightPx,
            RenderMode = entity.RenderMode,
            Priority = entity.Priority,
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
}
