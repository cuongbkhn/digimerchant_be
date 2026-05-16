using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Auth;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Options;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace DigiMerchantBE.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<DmUser> _passwordHasher;
    private readonly IUserHistoryService _userHistoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoleService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtOptions _jwtOptions;
    private readonly RefreshTokenCookieOptions _refreshTokenCookieOptions;

    public AuthService(
        AppDbContext dbContext,
        ITokenService tokenService,
        IPasswordHasher<DmUser> passwordHasher,
        IUserHistoryService userHistoryService,
        ICurrentUserService currentUserService,
        IRoleService roleService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<JwtOptions> jwtOptions,
        IOptions<RefreshTokenCookieOptions> refreshTokenCookieOptions)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _userHistoryService = userHistoryService;
        _currentUserService = currentUserService;
        _roleService = roleService;
        _httpContextAccessor = httpContextAccessor;
        _jwtOptions = jwtOptions.Value;
        _refreshTokenCookieOptions = refreshTokenCookieOptions.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedUsername);

        if (user is null)
        {
            throw new ApiException(ApiErrorCodes.InvalidCredentials);
        }

        if (user.Status == 0)
        {
            throw new ApiException(ApiErrorCodes.AccountInactive);
        }

        if (user.Status == 2 || (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow))
        {
            throw new ApiException(ApiErrorCodes.AccountLocked);
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            user.NumberOfFailedLogins += 1;
            user.UpdatedDate = DateTime.UtcNow;

            if (user.NumberOfFailedLogins >= 5)
            {
                user.LockoutEndAt = DateTime.UtcNow.AddMinutes(15);
            }

            await _dbContext.SaveChangesAsync();
            await _userHistoryService.WriteAsync(user.UserId, user.UserName, "LOGIN_FAILED", "Đăng nhập thất bại");
            throw new ApiException(ApiErrorCodes.InvalidCredentials);
        }

        if (user.Role is null || !string.Equals(user.Role.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.InvalidRoleAssignment);
        }

        var functions = await GetRoleFunctionsAsync(user.RoleId);

        var accessTokenResult = _tokenService.GenerateAccessToken(user, user.Role, functions);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);
        var now = DateTime.UtcNow;
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

        user.NumberOfFailedLogins = 0;
        user.LastLoginTime = now;
        user.LockoutEndAt = null;
        user.UpdatedDate = now;

        _dbContext.RefreshTokens.Add(new DmRefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshTokenHash,
            JwtId = accessTokenResult.JwtId,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();
        SetRefreshTokenCookie(refreshToken, refreshTokenExpiresAt);
        await _userHistoryService.WriteAsync(user.UserId, user.UserName, "LOGIN", "Đăng nhập thành công");

        return new LoginResponse
        {
            AccessToken = accessTokenResult.AccessToken,
            ExpiresIn = _jwtOptions.AccessTokenMinutes * 60,
            User = BuildCurrentUserResponse(user, functions)
        }.WithSuccess();
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync()
    {
        var rawRefreshToken = GetRefreshTokenFromCookie();
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new ApiException(ApiErrorCodes.RefreshTokenInvalid);
        }

        var tokenHash = _tokenService.HashToken(rawRefreshToken.Trim());
        var now = DateTime.UtcNow;

        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x!.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (refreshToken is null || refreshToken.RevokedAt.HasValue || refreshToken.ExpiresAt <= now)
        {
            ClearRefreshTokenCookie();
            throw new ApiException(ApiErrorCodes.RefreshTokenInvalid);
        }

        var user = refreshToken.User;
        if (user is null || user.Status != 1 || (user.LockoutEndAt.HasValue && user.LockoutEndAt > now))
        {
            throw new ApiException(ApiErrorCodes.UserNotActive);
        }

        if (user.Role is null || !string.Equals(user.Role.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ApiErrorCodes.InvalidRoleAssignment);
        }

        var functions = await GetRoleFunctionsAsync(user.RoleId);
        var accessTokenResult = _tokenService.GenerateAccessToken(user, user.Role, functions);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

        refreshToken.RevokedAt = now;
        refreshToken.ReplacedByTokenHash = newRefreshTokenHash;

        _dbContext.RefreshTokens.Add(new DmRefreshToken
        {
            UserId = user.UserId,
            TokenHash = newRefreshTokenHash,
            JwtId = accessTokenResult.JwtId,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();
        SetRefreshTokenCookie(newRefreshToken, refreshTokenExpiresAt);

        return new RefreshTokenResponse
        {
            AccessToken = accessTokenResult.AccessToken,
            ExpiresIn = _jwtOptions.AccessTokenMinutes * 60
        }.WithSuccess();
    }

    public async Task LogoutAsync()
    {
        var refreshToken = GetRefreshTokenFromCookie();
        DmRefreshToken? tokenEntity = null;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = _tokenService.HashToken(refreshToken.Trim());
            tokenEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
            if (tokenEntity is not null && !tokenEntity.RevokedAt.HasValue)
            {
                tokenEntity.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        ClearRefreshTokenCookie();

        await _userHistoryService.WriteAsync(
            _currentUserService.UserId ?? tokenEntity?.UserId,
            _currentUserService.UserName,
            "LOGOUT",
            "Đăng xuất");
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new ApiException(ApiErrorCodes.Unauthorized);
        }

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == currentUserId.Value);

        if (user is null || user.Status != 1 || user.Role is null)
        {
            throw new ApiException(ApiErrorCodes.CurrentUserNotFound);
        }

        var functions = await GetRoleFunctionsAsync(user.RoleId);
        return BuildCurrentUserResponse(user, functions);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var actorRole = await _roleService.GetActorRoleAsync();
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserName.ToUpper() == normalizedUsername);
        if (user is null)
        {
            throw new ApiException(ApiErrorCodes.ResetPasswordUserNotFound);
        }

        await _roleService.EnsureCanResetPasswordAsync(actorRole, user);

        var rawPassword = GenerateTemporaryPassword(12);
        user.PasswordHash = _passwordHasher.HashPassword(user, rawPassword);
        user.NumberOfFailedLogins = 0;
        user.LockoutEndAt = null;
        user.IsPasswordChanged = 0;
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedUser = _currentUserService.UserName;

        await _dbContext.SaveChangesAsync();
        await _userHistoryService.WriteAsync(
            _currentUserService.UserId,
            _currentUserService.UserName,
            "RESET_PASSWORD",
            $"Reset mật khẩu cho user {user.UserName}",
            "DMBO_USER");

        return new ResetPasswordResponse
        {
            Username = user.UserName,
            RawPassword = rawPassword
        }.WithSuccess();
    }

    private async Task<IReadOnlyCollection<DmFunction>> GetRoleFunctionsAsync(long roleId)
    {
        return await _dbContext.RoleFunctions
            .Where(x => x.RoleId == roleId)
            .Join(
                _dbContext.Functions.Where(f => f.Status == "ACTIVE"),
                roleFunc => roleFunc.FunctionId,
                function => function.FunctionId,
                (_, function) => function)
            .ToArrayAsync();
    }

    private CurrentUserResponse BuildCurrentUserResponse(DmUser user, IReadOnlyCollection<DmFunction> functions)
    {
        return new CurrentUserResponse
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleCode = user.Role?.RoleCode ?? string.Empty,
            RoleName = user.Role?.RoleName ?? string.Empty,
            Functions = functions
                .Select(x => new UserFunctionDto
                {
                    FunctionId = x.FunctionId,
                    FunctionCode = x.FunctionCode,
                    FunctionName = x.FunctionName,
                    FunctionUrl = x.FunctionUrl,
                    FunctionDisplay = x.FunctionDisplay
                })
                .ToArray()
        };
    }

    private string? GetRefreshTokenFromCookie()
    {
        var cookieName = string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Name)
            ? "refresh_token"
            : _refreshTokenCookieOptions.Name;
        return _httpContextAccessor.HttpContext?.Request.Cookies[cookieName];
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new ApiException(ApiErrorCodes.CookieSetupFailed);

        var cookieName = string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Name)
            ? "refresh_token"
            : _refreshTokenCookieOptions.Name;
        var sameSite = _refreshTokenCookieOptions.GetSameSiteMode();
        var secure = ResolveSecureFlag(httpContext, sameSite);

        var options = new CookieOptions
        {
            HttpOnly = _refreshTokenCookieOptions.HttpOnly,
            Secure = secure,
            SameSite = sameSite,
            Expires = new DateTimeOffset(expiresAtUtc),
            Path = string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Path) ? "/" : _refreshTokenCookieOptions.Path,
            IsEssential = true
        };

        if (!string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Domain))
        {
            options.Domain = _refreshTokenCookieOptions.Domain;
        }

        httpContext.Response.Cookies.Append(cookieName, refreshToken, options);
    }

    private void ClearRefreshTokenCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var cookieName = string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Name)
            ? "refresh_token"
            : _refreshTokenCookieOptions.Name;
        var sameSite = _refreshTokenCookieOptions.GetSameSiteMode();
        var secure = ResolveSecureFlag(httpContext, sameSite);

        var options = new CookieOptions
        {
            HttpOnly = _refreshTokenCookieOptions.HttpOnly,
            Secure = secure,
            SameSite = sameSite,
            Path = string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Path) ? "/" : _refreshTokenCookieOptions.Path
        };

        if (!string.IsNullOrWhiteSpace(_refreshTokenCookieOptions.Domain))
        {
            options.Domain = _refreshTokenCookieOptions.Domain;
        }

        httpContext.Response.Cookies.Delete(cookieName, options);
    }

    private bool ResolveSecureFlag(HttpContext context, SameSiteMode sameSiteMode)
    {
        var securePolicy = _refreshTokenCookieOptions.GetSecurePolicy();
        var secure = securePolicy switch
        {
            CookieSecurePolicy.Always => true,
            CookieSecurePolicy.None => false,
            _ => context.Request.IsHttps
        };

        // Browser requires Secure=true when SameSite=None.
        if (sameSiteMode == SameSiteMode.None)
        {
            secure = true;
        }

        return secure;
    }

    private static string GenerateTemporaryPassword(int length)
    {
        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = charset[RandomNumberGenerator.GetInt32(0, charset.Length)];
        }

        return new string(chars);
    }
}
