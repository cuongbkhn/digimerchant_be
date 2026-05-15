namespace DigiMerchantBE.Common;

public class ApiResponse<T>
{
    public string ErrorCode { get; set; } = "00";
    public string Message { get; set; } = "Success";
    public T? Data { get; set; }
}
