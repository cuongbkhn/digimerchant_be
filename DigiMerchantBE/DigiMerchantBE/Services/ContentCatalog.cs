using DigiMerchantBE.Common;
using DigiMerchantBE.Options;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Services;

public class ContentCatalog : IContentCatalog
{
    private readonly IOptionsMonitor<ContentCatalogOptions> _options;

    public ContentCatalog(IOptionsMonitor<ContentCatalogOptions> options)
    {
        _options = options;
    }

    public string BannerDeletedStatus => NormalizeCode(_options.CurrentValue.Banner.DeletedStatus) ?? "DELETED";

    public string IconDeletedStatus => NormalizeCode(_options.CurrentValue.Icon.DeletedStatus) ?? "DELETED";

    public bool IsBannerWritableStatus(string status) =>
        Contains(_options.CurrentValue.Banner.WritableStatuses, status);

    public bool IsBannerChangeStatus(string status) =>
        Contains(_options.CurrentValue.Banner.ChangeStatuses, status);

    public bool IsBannerRenderMode(string renderMode) =>
        Contains(_options.CurrentValue.Banner.RenderModes, renderMode);

    public bool TryGetBannerAspectRatio(string aspectRatioCode, out decimal value)
    {
        value = default;
        var code = NormalizeCode(aspectRatioCode);
        if (code is null)
        {
            return false;
        }

        var map = _options.CurrentValue.Banner.AspectRatios;
        if (map is null || map.Count == 0)
        {
            return false;
        }

        foreach (var pair in map)
        {
            if (string.Equals(pair.Key, code, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        return false;
    }

    public bool IsIconWritableStatus(string status) =>
        Contains(_options.CurrentValue.Icon.WritableStatuses, status);

    public bool IsIconChangeStatus(string status) =>
        Contains(_options.CurrentValue.Icon.ChangeStatuses, status);

    public bool IsPlatform(string platform) =>
        Contains(_options.CurrentValue.Shared.Platforms, platform);

    public bool IsActionType(string actionType) =>
        Contains(_options.CurrentValue.Shared.ActionTypes, actionType);

    public string ResolveFunctionCodeFilterStatus(string? status)
    {
        var cfg = _options.CurrentValue.Icon.FunctionCodes;
        if (string.IsNullOrWhiteSpace(status))
        {
            return NormalizeCode(cfg.DefaultFilterStatus) ?? "ACTIVE";
        }

        var normalized = status.Trim().ToUpperInvariant();
        if (!Contains(cfg.AllowedFilterStatuses, normalized))
        {
            throw new ApiException(
                ApiErrorCodes.IconInvalid,
                $"STATUS lọc function-codes không hợp lệ. Cho phép: {string.Join(", ", cfg.AllowedFilterStatuses)}");
        }

        return normalized;
    }

    private static bool Contains(IEnumerable<string> allowed, string value)
    {
        var normalized = NormalizeCode(value);
        if (normalized is null)
        {
            return false;
        }

        foreach (var item in allowed)
        {
            if (string.Equals(NormalizeCode(item), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
