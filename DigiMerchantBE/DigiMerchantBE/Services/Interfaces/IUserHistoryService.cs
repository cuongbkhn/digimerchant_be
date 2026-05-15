namespace DigiMerchantBE.Services.Interfaces;

public interface IUserHistoryService
{
    Task WriteAsync(
        long? userId,
        string? username,
        string actionType,
        string actionDesc,
        string? editTable = null,
        string? oldValue = null,
        string? newValue = null,
        long? functionId = null,
        string? funcName = null);
}
