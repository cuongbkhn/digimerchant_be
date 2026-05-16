namespace DigiMerchantBE.Common;

public interface IApiResult
{
    string ErrorCode { get; set; }
    string ErrorDescription { get; set; }
}
