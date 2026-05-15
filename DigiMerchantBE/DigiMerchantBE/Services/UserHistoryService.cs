using DigiMerchantBE.Data;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;

namespace DigiMerchantBE.Services;

public class UserHistoryService : IUserHistoryService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserHistoryService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(
        long? userId,
        string? username,
        string actionType,
        string actionDesc,
        string? editTable = null,
        string? oldValue = null,
        string? newValue = null,
        long? functionId = null,
        string? funcName = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var history = new DmUserHistory
        {
            UserId = userId,
            UserName = username,
            ActionType = actionType,
            ActionDesc = actionDesc,
            EditTable = editTable,
            OldValue = oldValue,
            NewValue = newValue,
            FunctionId = functionId,
            FuncName = funcName,
            IpAddress = ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            ActionDate = DateTime.UtcNow
        };

        _dbContext.UserHistories.Add(history);
        await _dbContext.SaveChangesAsync();
    }
}
