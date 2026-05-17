namespace DigiMerchantBE.Options;

public class ServerOptions
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5141;
    public bool UseHttps { get; set; }
    public int HttpsPort { get; set; } = 7295;
}
