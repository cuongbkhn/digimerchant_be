using DigiMerchantBE.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigiMerchantBE.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DmUser> Users => Set<DmUser>();
    public DbSet<DmRole> Roles => Set<DmRole>();
    public DbSet<DmFunction> Functions => Set<DmFunction>();
    public DbSet<DmRoleFunc> RoleFunctions => Set<DmRoleFunc>();
    public DbSet<DmRefreshToken> RefreshTokens => Set<DmRefreshToken>();
    public DbSet<DmUserHistory> UserHistories => Set<DmUserHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DmUser>(entity =>
        {
            entity.ToTable("DMBO_USER");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.UserId).HasColumnName("USER_ID").ValueGeneratedOnAdd();
            entity.Property(x => x.UserName).HasColumnName("USER_NAME").HasMaxLength(100).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("FULL_NAME").HasMaxLength(255);
            entity.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(255);
            entity.Property(x => x.Phone).HasColumnName("PHONE").HasMaxLength(50);
            entity.Property(x => x.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(500).IsRequired();
            entity.Property(x => x.PwdExpireDate).HasColumnName("PWD_EXPIRE_DATE");
            entity.Property(x => x.RoleId).HasColumnName("ROLE_ID").IsRequired();
            entity.Property(x => x.Uuid).HasColumnName("UUID").HasMaxLength(100);
            entity.Property(x => x.NumberOfFailedLogins).HasColumnName("NUMBER_OF_FAILED_LOGINS").IsRequired();
            entity.Property(x => x.LastLoginTime).HasColumnName("LAST_LOGIN_TIME");
            entity.Property(x => x.LockoutEndAt).HasColumnName("LOCKOUT_END_AT");
            entity.Property(x => x.IsPasswordChanged).HasColumnName("IS_PASSWORD_CHANGED").IsRequired();
            entity.Property(x => x.CreatedDate).HasColumnName("CREATED_DATE").IsRequired();
            entity.Property(x => x.CreatedUser).HasColumnName("CREATED_USER").HasMaxLength(100);
            entity.Property(x => x.UpdatedDate).HasColumnName("UPDATED_DATE");
            entity.Property(x => x.UpdatedUser).HasColumnName("UPDATED_USER").HasMaxLength(100);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<DmRole>(entity =>
        {
            entity.ToTable("DMBO_ROLE");
            entity.HasKey(x => x.RoleId);

            entity.Property(x => x.RoleId).HasColumnName("ROLE_ID").ValueGeneratedOnAdd();
            entity.Property(x => x.RoleCode).HasColumnName("ROLE_CODE").HasMaxLength(100).IsRequired();
            entity.Property(x => x.RoleName).HasColumnName("ROLE_NAME").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
            entity.Property(x => x.Status).HasColumnName("STATUS").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("CREATED_BY").HasMaxLength(100);
            entity.Property(x => x.CreateTime).HasColumnName("CREATE_TIME").IsRequired();
            entity.Property(x => x.UpdatedBy).HasColumnName("UPDATED_BY").HasMaxLength(100);
            entity.Property(x => x.UpdateTime).HasColumnName("UPDATE_TIME");
        });

        modelBuilder.Entity<DmFunction>(entity =>
        {
            entity.ToTable("DMBO_FUNCTION");
            entity.HasKey(x => x.FunctionId);

            entity.Property(x => x.FunctionId).HasColumnName("FUNCTION_ID").ValueGeneratedOnAdd();
            entity.Property(x => x.FunctionCode).HasColumnName("FUNCTION_CODE").HasMaxLength(150).IsRequired();
            entity.Property(x => x.FunctionName).HasColumnName("FUNCTION_NAME").HasMaxLength(255).IsRequired();
            entity.Property(x => x.FunctionLevel).HasColumnName("FUNCTION_LEVEL").IsRequired();
            entity.Property(x => x.FunctionUrl).HasColumnName("FUNCTION_URL").HasMaxLength(500);
            entity.Property(x => x.FunctionOrder).HasColumnName("FUNCTION_ORDER").IsRequired();
            entity.Property(x => x.ParentId).HasColumnName("PARENT_ID");
            entity.Property(x => x.FunctionDisplay).HasColumnName("FUNCTION_DISPLAY").IsRequired();
            entity.Property(x => x.Status).HasColumnName("STATUS").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("CREATED_BY").HasMaxLength(100);
            entity.Property(x => x.CreateTime).HasColumnName("CREATE_TIME").IsRequired();
            entity.Property(x => x.UpdatedBy).HasColumnName("UPDATED_BY").HasMaxLength(100);
            entity.Property(x => x.UpdateTime).HasColumnName("UPDATE_TIME");

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId);
        });

        modelBuilder.Entity<DmRoleFunc>(entity =>
        {
            entity.ToTable("DMBO_ROLE_FUNC");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.FunctionId).HasColumnName("FUNCTION_ID").IsRequired();
            entity.Property(x => x.RoleId).HasColumnName("ROLE_ID").IsRequired();
            entity.Property(x => x.CreateTime).HasColumnName("CREATE_TIME").IsRequired();
            entity.Property(x => x.CreateUser).HasColumnName("CREATE_USER").HasMaxLength(100);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.RoleFunctions)
                .HasForeignKey(x => x.RoleId);

            entity.HasOne(x => x.Function)
                .WithMany(x => x.RoleFunctions)
                .HasForeignKey(x => x.FunctionId);
        });

        modelBuilder.Entity<DmRefreshToken>(entity =>
        {
            entity.ToTable("DMBO_REFRESH_TOKEN");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("USER_ID").IsRequired();
            entity.Property(x => x.TokenHash).HasColumnName("TOKEN_HASH").HasMaxLength(500).IsRequired();
            entity.Property(x => x.JwtId).HasColumnName("JWT_ID").HasMaxLength(100);
            entity.Property(x => x.IpAddress).HasColumnName("IP_ADDRESS").HasMaxLength(100);
            entity.Property(x => x.UserAgent).HasColumnName("USER_AGENT").HasMaxLength(500);
            entity.Property(x => x.ExpiresAt).HasColumnName("EXPIRES_AT").IsRequired();
            entity.Property(x => x.RevokedAt).HasColumnName("REVOKED_AT");
            entity.Property(x => x.ReplacedByTokenHash).HasColumnName("REPLACED_BY_TOKEN_HASH").HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<DmUserHistory>(entity =>
        {
            entity.ToTable("DMBO_USER_HISTORIES");
            entity.HasKey(x => x.HistoryId);

            entity.Property(x => x.HistoryId).HasColumnName("HISTORY_ID").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("USER_ID");
            entity.Property(x => x.UserName).HasColumnName("USER_NAME").HasMaxLength(100);
            entity.Property(x => x.FunctionId).HasColumnName("FUNCTION_ID");
            entity.Property(x => x.FuncName).HasColumnName("FUNC_NAME").HasMaxLength(255);
            entity.Property(x => x.ActionType).HasColumnName("ACTION_TYPE").HasMaxLength(100).IsRequired();
            entity.Property(x => x.ActionDesc).HasColumnName("ACTION_DESC").HasMaxLength(1000);
            entity.Property(x => x.ActionDate).HasColumnName("ACTION_DATE").IsRequired();
            entity.Property(x => x.EditTable).HasColumnName("EDIT_TABLE").HasMaxLength(100);
            entity.Property(x => x.OldValue).HasColumnName("OLD_VALUE").HasColumnType("CLOB");
            entity.Property(x => x.NewValue).HasColumnName("NEW_VALUE").HasColumnType("CLOB");
            entity.Property(x => x.IpAddress).HasColumnName("IP_ADDRESS").HasMaxLength(100);
            entity.Property(x => x.UserAgent).HasColumnName("USER_AGENT").HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserHistories)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);

            entity.HasOne(x => x.Function)
                .WithMany(x => x.UserHistories)
                .HasForeignKey(x => x.FunctionId)
                .IsRequired(false);
        });
    }
}
