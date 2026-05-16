namespace DigiMerchantBE.Common;

public static class AppEnvironments
{
    public const string Uat = "UAT";
    public const string Pilot = "PILOT";
    public const string Prod = "PROD";

    public const string PlatformAll = "ALL";

    public static readonly string[] All = [Uat, Pilot, Prod];
}
