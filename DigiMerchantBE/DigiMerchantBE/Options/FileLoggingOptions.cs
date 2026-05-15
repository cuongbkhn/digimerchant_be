namespace DigiMerchantBE.Options;

public class FileLoggingOptions
{
    public string FilePath { get; set; } = "logs/digimerchant-.log";
    public int MaxFileSizeMb { get; set; } = 20;
    public int RetainDays { get; set; } = 30;
    public string MinimumLevel { get; set; } = "Information";
}
