using System.Text.RegularExpressions;
using DigiMerchantBE.Common;
using DigiMerchantBE.Data;
using DigiMerchantBE.DTOs.Users;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Services;

public partial class UserService : IUserService
{
    private static readonly Regex UsernamePattern = UsernameRegex();

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<DmUser> _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserHistoryService _userHistoryService;
    private readonly IRoleService _roleService;

    public UserService(
        AppDbContext dbContext,
        IPasswordHasher<DmUser> passwordHasher,
        ICurrentUserService currentUserService,
        IUserHistoryService userHistoryService,
        IRoleService roleService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _userHistoryService = userHistoryService;
        _roleService = roleService;
    }

    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request)
    {
        var actorRole = await _roleService.GetActorRoleAsync();
        var targetRoleCode = request.RoleCode.Trim();

        var userName = request.UserName.Trim();
        if (!UsernamePattern.IsMatch(userName))
        {
            throw new ApiException(ApiErrorCodes.InvalidUsernameFormat);
        }

        var normalizedUsername = userName.ToUpperInvariant();
        var exists = await _dbContext.Users.AnyAsync(x => x.UserName.ToUpper() == normalizedUsername);
        if (exists)
        {
            throw new ApiException(ApiErrorCodes.UsernameAlreadyExists);
        }

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.RoleCode.ToUpper() == targetRoleCode.ToUpperInvariant() && x.Status == "ACTIVE");

        if (role is null)
        {
            throw new ApiException(ApiErrorCodes.RoleNotFoundOrInactive);
        }

        await _roleService.EnsureCanAssignRoleAsync(actorRole, role);

        var now = DateTime.UtcNow;
        var user = new DmUser
        {
            UserName = userName,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Status = 1,
            RoleId = role.RoleId,
            Uuid = Guid.NewGuid().ToString("N"),
            NumberOfFailedLogins = 0,
            IsPasswordChanged = 1,
            CreatedDate = now,
            CreatedUser = _currentUserService.UserName
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        await _userHistoryService.WriteAsync(
            _currentUserService.UserId,
            _currentUserService.UserName,
            "USER_REGISTER",
            $"Tạo user {user.UserName} với role {role.RoleCode}",
            "DMBO_USER");

        return new RegisterUserResponse
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName
        }.WithSuccess();
    }

    [GeneratedRegex("^[a-zA-Z0-9_]{3,100}$")]
    private static partial Regex UsernameRegex();
}
