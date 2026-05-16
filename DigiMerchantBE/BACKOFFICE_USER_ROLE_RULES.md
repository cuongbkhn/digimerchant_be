# DigiMerchant BackOffice — Quy tắc User & Role

Tài liệu mô tả quy tắc phân quyền cho API quản lý user. Thứ bậc role lấy từ cột **`DMBO_ROLE.ROLE_LEVEL`** (số càng nhỏ = quyền càng cao).

| ROLE_CODE     | ROLE_LEVEL | Ghi chú              |
|---------------|------------|----------------------|
| SUPER_ADMIN   | 0          | Không tạo qua API    |
| ADMIN         | 1          |                      |
| OPERATOR      | 2          |                      |
| VIEWER        | 3          |                      |

**Nguyên tắc chung:** User chỉ được thao tác với user/role có `ROLE_LEVEL` **lớn hơn** (tức cấp thấp hơn) role của mình. Không thao tác với `SUPER_ADMIN` (trừ tài khoản seed ban đầu).

Tất cả API dưới đây yêu cầu JWT hợp lệ và function **`USER_MANAGEMENT`**.

---

## 1. Đăng ký / tạo user

**API:** `POST /api/users/register`

**API dropdown role:** `GET /api/roles` — danh sách role được phép gán (không có `SUPER_ADMIN`).

### Quy tắc

| Người thực hiện | Được tạo user với role |
|-----------------|------------------------|
| **SUPER_ADMIN** | `ADMIN`, `OPERATOR`, `VIEWER` (và mọi role ACTIVE trong DB có `ROLE_LEVEL > 0`) |
| **ADMIN**       | `OPERATOR`, `VIEWER` (và mọi role ACTIVE có `ROLE_LEVEL > 1`) |
| **OPERATOR**    | Không tạo được (không đủ cấp / thường không có `USER_MANAGEMENT`) |
| **VIEWER**      | Không tạo được |

### Cấm

- Tạo tài khoản **`SUPER_ADMIN`** qua API (`errorCode: 11`).
- Gán role có `ROLE_LEVEL` ≤ role của người tạo (`errorCode: 11`).

### Ví dụ

- Super Admin tạo user `admin_biz` role **ADMIN** → được.
- Admin tạo user `op01` role **OPERATOR** → được.
- Admin tạo user role **ADMIN** hoặc **SUPER_ADMIN** → **403**.

---

## 2. Reset mật khẩu

**API:** `POST /api/auth/reset-password`  
**Body:** `{ "username": "..." }`

### Quy tắc

| Người thực hiện | Được reset mật khẩu cho |
|-----------------|-------------------------|
| **SUPER_ADMIN** | `ADMIN`, `OPERATOR`, `VIEWER` (user có `ROLE_LEVEL > 0`) |
| **ADMIN**       | `OPERATOR`, `VIEWER` (user có `ROLE_LEVEL > 1`) |
| **OPERATOR**    | Không reset được (theo phân cấp) |
| **VIEWER**      | Không reset được |

### Cấm

- Reset mật khẩu **chính mình** (`errorCode: 16`).
- Reset cho user **SUPER_ADMIN** (`errorCode: 11`).
- Reset cho user cùng cấp hoặc cao hơn (ví dụ Admin reset Admin / Super Admin) (`errorCode: 11`).

### Ví dụ

- Super Admin reset pass user **ADMIN** → được.
- Super Admin reset pass user **OPERATOR** → được.
- Admin reset pass user **OPERATOR** → được.
- Admin reset pass user **ADMIN** hoặc **chính mình** → **403**.

---

## 3. Mã lỗi liên quan

Danh sách đầy đủ `errorCode` + `errorDescription` nằm trong source:

- `DigiMerchantBE/Common/ApiErrorCodes.cs` — Auth, User, Role
- `DigiMerchantBE/Common/CryptoErrorCodes.cs` — Mã hóa (CRxxx)

Response API dùng cặp field: **`errorCode`**, **`errorDescription`** (thay cho `message` cũ).

| errorCode | HTTP | Ý nghĩa (tóm tắt) |
|-----------|------|-------------------|
| `11` | 403 | Không đủ quyền theo phân cấp role |
| `12` | 400 | Role không hợp lệ / user chưa có role |
| `13` | 409 | Username đã tồn tại (register) |
| `14` | 403 | Không xác định role người gọi |
| `15` | 400 | Username không đúng định dạng |
| `16` | 403 | Không được reset mật khẩu chính mình |
| `10` | 404 | Không tìm thấy user (reset) |

---

## 4. Database

- Script gốc: `sql/DM_BACKOFFICE_AUTH_RBAC_V2_ORACLE.sql` (có `ROLE_LEVEL`).
- User đầu tiên: `sql/DM_BACKOFFICE_SEED_SUPER_ADMIN_USER.sql`.
- Nếu DB cũ chưa có cột `ROLE_LEVEL`: chạy `sql/DM_BACKOFFICE_ALTER_ROLE_LEVEL.sql`.

Role mới thêm vào `DMBO_ROLE` cần set `ROLE_LEVEL` phù hợp để API phân quyền tự áp dụng, không cần sửa code.

---

## 5. Implementation (tham chiếu code)

- Phân cấp role: `RoleService.EnsureSubordinateRole`
- Đăng ký user: `UserService.RegisterUserAsync` → `IRoleService.EnsureCanAssignRoleAsync`
- Reset password: `AuthService.ResetPasswordAsync` → `IRoleService.EnsureCanResetPasswordAsync`
- Danh sách role dropdown: `RoleService.GetRegisterableRolesAsync`
