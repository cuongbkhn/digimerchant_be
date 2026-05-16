using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Banners;
using DigiMerchantBE.DTOs.Icons;
using DigiMerchantBE.DTOs.MobileConfig;

using DigiMerchantBE.Entities;

using DigiMerchantBE.Services.Interfaces;

using Microsoft.EntityFrameworkCore;



namespace DigiMerchantBE.Services;



public class MobileConfigService : IMobileConfigService

{

    private readonly AppDbContext _dbContext;



    public MobileConfigService(AppDbContext dbContext)

    {

        _dbContext = dbContext;

    }



    public Task<MobileBootstrapConfigResponse> GetPublicBootstrapAsync(string environmentCode, MobileConfigRequest request) =>

        BuildBootstrapAsync(environmentCode, request, publicOnly: true);



    public Task<MobileBootstrapConfigResponse> GetAuthenticatedBootstrapAsync(string environmentCode, MobileConfigRequest request) =>

        BuildBootstrapAsync(environmentCode, request, publicOnly: false);



    private async Task<MobileBootstrapConfigResponse> BuildBootstrapAsync(

        string environmentCode,

        MobileConfigRequest request,

        bool publicOnly)

    {

        var env = environmentCode.Trim().ToUpperInvariant();

        var now = DateTime.UtcNow;



        var banners = await _dbContext.BannerConfigs.AsNoTracking()

            .Where(x => x.EnvironmentCode == env && x.Status == "ACTIVE")

            .Where(x => x.StartTime == null || x.StartTime <= now)

            .Where(x => x.EndTime == null || x.EndTime >= now)

            .ToListAsync();



        var icons = await _dbContext.IconConfigs.AsNoTracking()

            .Where(x => x.EnvironmentCode == env && x.Status == "ACTIVE")

            .Where(x => x.StartTime == null || x.StartTime <= now)

            .Where(x => x.EndTime == null || x.EndTime >= now)

            .ToListAsync();



        if (publicOnly)

        {

            banners = banners.Where(x => !x.LoginRequired).ToList();

            icons = icons.Where(x => !x.LoginRequired).ToList();

        }



        banners = banners

            .Where(x => MobileConfigFilterHelper.MatchesPlatform(x.Platform, request.Platform))

            .Where(x => MobileConfigFilterHelper.MeetsAppVersion(x.AppVersion, request.AppVersion))

            .OrderBy(x => x.Priority)

            .ThenByDescending(x => x.CreatedAt)

            .ToList();



        icons = icons

            .Where(x => MobileConfigFilterHelper.MatchesPlatform(x.Platform, request.Platform))

            .Where(x => MobileConfigFilterHelper.MeetsAppVersion(x.AppVersion, request.AppVersion))

            .OrderBy(x => x.Priority)

            .ThenByDescending(x => x.CreatedAt)

            .ToList();



        var bannerResponses = banners.Select(MapBanner).ToList();

        var iconResponses = icons.Select(x => MapIcon(x, now)).ToList();

        var categoryItems = BuildIconCategories(icons);



        var maxUpdated = banners.Select(x => x.UpdatedAt ?? x.CreatedAt)

            .Concat(icons.Select(x => x.UpdatedAt ?? x.CreatedAt))

            .DefaultIfEmpty(now)

            .Max();



        return new MobileBootstrapConfigResponse

        {

            EnvironmentCode = env,

            ConfigVersion = $"{maxUpdated:yyyyMMddHHmmss}-{bannerResponses.Count}-{iconResponses.Count}",

            ServerTime = now,

            Banners = bannerResponses,

            IconCategories = categoryItems,

            Icons = iconResponses

        };

    }



    private static List<MobileIconCategoryItem> BuildIconCategories(List<DmIconConfig> icons) =>

        icons

            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryCode))

            .GroupBy(x => new

            {

                Group = x.GroupCode.Trim().ToUpperInvariant(),

                Category = x.CategoryCode!.Trim().ToUpperInvariant(),

                Type = x.TypeCode?.Trim().ToUpperInvariant()

            })

            .Select(g =>

            {

                var first = g.OrderBy(x => x.Priority).ThenBy(x => x.IconId).First();

                return new MobileIconCategoryItem

                {

                    GroupCode = first.GroupCode,

                    CategoryCode = first.CategoryCode!,

                    TypeCode = first.TypeCode,

                    CategoryName = first.CategoryCode!,

                    Priority = g.Min(x => x.Priority)

                };

            })

            .OrderBy(x => x.Priority)

            .ThenBy(x => x.CategoryName)

            .ToList();



    private static MobileBannerResponse MapBanner(DmBannerConfig entity) =>

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

            TrackingCode = entity.TrackingCode

        };



    private static MobileIconResponse MapIcon(DmIconConfig entity, DateTime now) =>

        new()

        {

            IconId = entity.IconId,

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

            Badge = BuildBadge(entity, now),

            Priority = entity.Priority,

            GridSpan = entity.GridSpan,

            TrackingCode = entity.TrackingCode

        };



    private static MobileIconBadgeDto? BuildBadge(DmIconConfig entity, DateTime now)

    {

        if (string.IsNullOrWhiteSpace(entity.BadgeType) && string.IsNullOrWhiteSpace(entity.BadgeText))

        {

            return null;

        }



        if (entity.BadgeStartTime.HasValue && entity.BadgeStartTime > now)

        {

            return null;

        }



        if (entity.BadgeEndTime.HasValue && entity.BadgeEndTime < now)

        {

            return null;

        }



        return new MobileIconBadgeDto

        {

            Type = entity.BadgeType,

            Text = entity.BadgeText,

            Color = entity.BadgeColor,

            BackgroundColor = entity.BadgeBgColor

        };

    }

}

