namespace DigiMerchantBE.Common;

public static class MobileConfigFilterHelper
{
    public static bool IsWithinSchedule(DateTime? startTime, DateTime? endTime, DateTime now) =>
        (startTime == null || startTime <= now) && (endTime == null || endTime >= now);

    public static bool MatchesPlatform(string entityPlatform, string? requestedPlatform)
    {
        if (string.IsNullOrWhiteSpace(requestedPlatform))
        {
            return true;
        }

        var platform = entityPlatform.Trim().ToUpperInvariant();
        var requested = requestedPlatform.Trim().ToUpperInvariant();
        return platform == AppEnvironments.PlatformAll || platform == requested;
    }

    /// <summary>
    /// Client appVersion phải &gt;= AppVersion cấu hình (nếu có). Null/empty cấu hình = luôn hiển thị.
    /// </summary>
    public static bool MeetsAppVersion(string? requiredVersion, string? clientAppVersion)
    {
        if (string.IsNullOrWhiteSpace(requiredVersion))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(clientAppVersion))
        {
            return false;
        }

        return CompareAppVersions(clientAppVersion, requiredVersion) >= 0;
    }

    public static int CompareAppVersions(string left, string right)
    {
        if (Version.TryParse(NormalizeVersion(left), out var l) && Version.TryParse(NormalizeVersion(right), out var r))
        {
            return l.CompareTo(r);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string version) => version.Trim().Split('-')[0];
}
