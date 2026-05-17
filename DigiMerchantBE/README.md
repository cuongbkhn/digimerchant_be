# DigiMerchant BackOffice API

**Hướng dẫn tích hợp Web BO (mã hóa, API, request/response):** [docs/BACKOFFICE_WEB_INTEGRATION.md](docs/BACKOFFICE_WEB_INTEGRATION.md)

**Port khi chạy:** `appsettings.json` → `Server.Port` (mặc định `5141`). Host `Server.Host` (`0.0.0.0` = listen mọi interface). Có thể ghi đè bằng biến môi trường `ASPNETCORE_URLS`.

Base path: `/api` (trừ khi ghi chú khác).

**Auth:** Các endpoint không ghi `Public` cần JWT Bearer. Backoffice banner/icon dùng body mã hóa (crypto envelope) khi client bật mã hóa.

**Môi trường:** `UAT`, `PILOT`, `PROD` — client truyền `environmentCode` (query hoặc trong body tùy API), không có bảng môi trường riêng.

**Danh mục mã (status, platform, action type, …):** cấu hình trong `appsettings.json` → section `ContentCatalog` (có thể sửa khi chạy, reload config). Banner/icon và API `function-codes` đọc từ đây.

---

## Auth — `/api/auth`

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/auth/login` | **Public.** Trả về access token, refresh token và thông tin đăng nhập. |
| POST | `/api/auth/refresh-token` | **Public.** Trả về cặp token mới (dùng refresh token từ cookie/header theo cấu hình app). |
| POST | `/api/auth/logout` | **Public.** Thu hồi refresh token; trả về thông báo đăng xuất thành công. |
| GET | `/api/auth/me` | Thông tin user đang đăng nhập (profile, role, quyền). |
| POST | `/api/auth/reset-password` | Mật khẩu mới cho user (theo quyền `USER_MANAGEMENT`). |

---

## Users — `/api/users`

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/users/register` | Tài khoản backoffice vừa tạo (userId, userName, …). |

---

## Roles — `/api/roles`

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/roles` | Danh sách role được phép gán khi đăng ký user (dropdown, không có SUPER_ADMIN). |

---

## Crypto — `/api/crypto`

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/crypto/public-key` | **Public.** Danh sách public key (kid) để client mã hóa payload. |

---

## OTT — `/api/ott`

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/ott/send-single` | Kết quả gửi OTT đơn (hiện stub; trả về receiver đã giải mã). |

---

## Banners (backoffice) — `/api/banners`

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/banners` | Trang danh sách banner (filter theo môi trường, group, status, …). |
| GET | `/api/banners/{id}` | Chi tiết một banner. |
| POST | `/api/banners` | Banner vừa tạo. |
| PUT | `/api/banners/{id}` | Banner sau khi cập nhật. |
| PUT | `/api/banners/{id}/status` | Trạng thái đã đổi (không trả entity). |
| DELETE | `/api/banners/{id}` | Xóa mềm banner (status `DELETED`). |

---

## Icons (backoffice) — `/api/icons`

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/icons` | Trang danh sách icon. |
| GET | `/api/icons/{iconId}` | Chi tiết một icon. |
| GET | `/api/icons/function-codes` | Danh sách `functionCode` từ icon (dùng chọn `mobileFunctionCodes` cho banner). |
| POST | `/api/icons` | Icon vừa tạo. |
| PUT | `/api/icons/{iconId}` | Icon sau khi cập nhật. |
| PUT | `/api/icons/{iconId}/status` | Trạng thái icon đã đổi. |
| DELETE | `/api/icons/{iconId}` | Xóa mềm icon. |

---

## Mobile config — `/api/mobile/config`

Query chung: `environmentCode` (bắt buộc), `platform`, `appVersion`, `configVersion` (tùy chọn).

**Lọc `appVersion`:** Mỗi banner/icon có thể cấu hình `appVersion` (phiên bản tối thiểu). Chỉ trả về item khi `appVersion` client **≥** giá trị cấu hình; nhỏ hơn thì bỏ qua. Không cấu hình `appVersion` trên item = hiển thị mọi phiên bản client.

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/mobile/config/public-bootstrap` | **Public.** Gói cấu hình cho guest: banner + nhóm category (suy từ icon) + icon không yêu cầu đăng nhập; kèm `configVersion`, `serverTime`. |
| GET | `/api/mobile/config/bootstrap` | Gói cấu hình đầy đủ cho user đã đăng nhập (gồm cả item `loginRequired`). |

**Nội dung bootstrap:** `environmentCode`, `configVersion`, `serverTime`, `banners[]`, `iconCategories[]` (group/category/type + priority), `icons[]`.

---

## Oracle scripts

Thứ tự chạy SQL: xem [sql/README.md](sql/README.md).
