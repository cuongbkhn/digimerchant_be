namespace DigiMerchantBE.Options;

public class ApiLoggingOptions
{
    public string DefaultMode { get; set; } = "RequestOnly";
    public int MaxBodyLength { get; set; } = 4096;
    public List<ApiLoggingRule> Rules { get; set; } = new();
}

public class ApiLoggingRule
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = "*";
    public string Path { get; set; } = "/";
    public string Mode { get; set; } = "RequestOnly";
}
