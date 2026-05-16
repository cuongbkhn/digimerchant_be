namespace DigiMerchantBE.Options;

public class ContentCatalogOptions
{
    public SharedContentCatalogOptions Shared { get; set; } = new();
    public BannerContentCatalogOptions Banner { get; set; } = new();
    public IconContentCatalogOptions Icon { get; set; } = new();
}

public class SharedContentCatalogOptions
{
    public List<string> Platforms { get; set; } = ["ALL", "IOS", "ANDROID"];
    public List<string> ActionTypes { get; set; } =
        ["NONE", "NATIVE", "DEEPLINK", "WEBVIEW", "EXTERNAL_BROWSER"];
}

public class BannerContentCatalogOptions
{
    public List<string> WritableStatuses { get; set; } = ["DRAFT", "ACTIVE", "INACTIVE", "EXPIRED"];
    public List<string> ChangeStatuses { get; set; } = ["DRAFT", "ACTIVE", "INACTIVE", "EXPIRED"];
    public string DeletedStatus { get; set; } = "DELETED";
    public List<string> RenderModes { get; set; } = ["FIT_WIDTH", "CENTER_CROP", "CONTAIN", "COVER"];
    public Dictionary<string, decimal> AspectRatios { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1_1"] = 1.0000m,
        ["16_9"] = 1.7778m,
        ["4_3"] = 1.3333m,
        ["3_1"] = 3.0000m,
        ["2_1"] = 2.0000m,
        ["9_16"] = 0.5625m
    };
}

public class IconContentCatalogOptions
{
    public List<string> WritableStatuses { get; set; } = ["DRAFT", "ACTIVE", "INACTIVE", "EXPIRED", "DELETED"];
    public List<string> ChangeStatuses { get; set; } = ["DRAFT", "ACTIVE", "INACTIVE", "EXPIRED"];
    public string DeletedStatus { get; set; } = "DELETED";
    public IconFunctionCodeCatalogOptions FunctionCodes { get; set; } = new();
}

public class IconFunctionCodeCatalogOptions
{
    public string DefaultFilterStatus { get; set; } = "ACTIVE";
    public List<string> AllowedFilterStatuses { get; set; } = ["ACTIVE", "DRAFT", "INACTIVE", "EXPIRED"];
}
