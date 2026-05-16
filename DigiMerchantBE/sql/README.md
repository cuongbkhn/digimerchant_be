# Oracle scripts — DigiMerchant BackOffice

Danh sách API HTTP: [../README.md](../README.md).

Chạy theo thứ tự (schema user Oracle đích):

1. `DM_BACKOFFICE_AUTH_RBAC_V2_ORACLE.sql` — RBAC + seed role/function
2. `DM_BACKOFFICE_ALTER_ROLE_LEVEL.sql` — chỉ khi DB cũ chưa có `ROLE_LEVEL`
3. `DM_BACKOFFICE_SEED_SUPER_ADMIN_USER.sql` — user `admin` / `Admin@123`
4. `DM_BANNER_CONFIG_ORACLE.sql` — bảng `DM_BANNER_CONFIG` + quyền banner
5. `DM_ICON_CONFIG_ORACLE.sql` — bảng `DM_ICON_CONFIG` + quyền icon
6. `DM_BANNER_ICON_APP_VERSION_ALTER.sql` — chỉ khi DB cũ còn `MIN_APP_VERSION` / `MAX_APP_VERSION`

Môi trường `UAT` / `PILOT` / `PROD`: không có bảng riêng — client truyền `environmentCode` trên API.

Không dùng: event log, daily stat, `DM_ICON_CATEGORY`, `DM_ENVIRONMENT`, `DM_APP_CLIENT`.

```powershell
dotnet run --project ../tools/GeneratePasswordHash -- "YourNewPassword"
```
