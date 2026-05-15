namespace DigiMerchantBE.Entities;

public class DmUser
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? PwdExpireDate { get; set; }
    public long RoleId { get; set; }
    public string? Uuid { get; set; }
    public int NumberOfFailedLogins { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public int IsPasswordChanged { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedUser { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedUser { get; set; }

    public DmRole? Role { get; set; }
    public ICollection<DmRefreshToken> RefreshTokens { get; set; } = new List<DmRefreshToken>();
    public ICollection<DmUserHistory> UserHistories { get; set; } = new List<DmUserHistory>();
}
