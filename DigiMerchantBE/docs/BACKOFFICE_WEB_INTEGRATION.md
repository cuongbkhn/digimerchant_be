# Hướng dẫn tích hợp Web BackOffice (Angular) với DigiMerchant BE

Tài liệu dành cho team FE / Cursor BO: cách gọi API, mã hóa payload, bật/tắt raw JSON, danh sách API và contract request/response.

---

## 1. Thông tin chung

| Mục | Giá trị |
|-----|---------|
| Base URL (dev) | `http://localhost:5141` (đổi trong `appsettings` → `Server.Port`) |
| Prefix API | `/api` |
| JSON naming | **camelCase** (`errorCode`, `environmentCode`, …) |
| CORS (dev) | `http://localhost:4200`, `AllowCredentials: true` |
| Swagger (Development) | `/swagger` |

### 1.1. Hai dạng response

**A. Auth / User / Role** — object phẳng có `errorCode`, `errorDescription` + field nghiệp vụ:

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "accessToken": "...",
  "expiresIn": 1800,
  "user": { ... }
}
```

**B. Banner / Icon / OTT (phần lớn)** — bọc trong `data`:

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "data": { ... }
}
```

**Lỗi** (middleware): HTTP 4xx/5xx, body:

```json
{
  "errorCode": "xx",
  "errorDescription": "Mô tả lỗi",
  "data": null
}
```

`errorCode === "00"` → thành công.

### 1.2. Xác thực JWT

1. `POST /api/auth/login` → nhận `accessToken`, `expiresIn` (giây), `user.functions[]`.
2. Gọi API bảo vệ: header `Authorization: Bearer {accessToken}`.
3. Refresh token nằm trong **HttpOnly cookie** `refresh_token` (tên cấu hình BE: `RefreshTokenCookie.Name`).
4. `POST /api/auth/refresh-token` — **không body**, gửi kèm cookie (`withCredentials: true`).
5. `POST /api/auth/logout` — xóa cookie phía server.

**Angular HttpClient:**

```typescript
// environment.ts
export const environment = {
  apiBaseUrl: 'http://localhost:5000',
  crypto: {
    enabled: true,           // false = gửi raw JSON (khi BE bật bypass)
    kid: 'rsa-key-2026-01',
    publicKeyPemPath: 'assets/keys/public.pem'  // file đã copy từ BE
  }
};
```

```typescript
this.http.post(`${apiBaseUrl}/api/auth/login`, body, { withCredentials: true });
this.http.get(`${apiBaseUrl}/api/banners`, {
  withCredentials: true,
  headers: { Authorization: `Bearer ${accessToken}` }
});
```

### 1.3. Môi trường cấu hình (UAT / PILOT / PROD)

- Mọi API banner/icon **bắt buộc** `environmentCode` (query hoặc body).
- Giá trị hợp lệ: `UAT`, `PILOT`, `PROD` (theo `MobileConfig.AllowedEnvironments` trên BE).
- User **không phải SUPER_ADMIN**: được thao tác **UAT, PILOT**; **không** được chọn **PROD**.
- **SUPER_ADMIN**: được cả 3.

UI gợi ý: dropdown môi trường global; disable `PROD` nếu `user.roleCode !== 'SUPER_ADMIN'`.

---

## 2. Mã hóa request (Crypto envelope)

### 2.1. API nào cần mã hóa?

Chỉ **POST/PUT có body** qua `ICryptoEnvelopeService`:

| API | Mã hóa khi `crypto.enabled === true` |
|-----|--------------------------------------|
| `POST /api/banners` | Có |
| `PUT /api/banners/{id}` | Có |
| `PUT /api/banners/{id}/status` | Có |
| `POST /api/icons` | Có |
| `PUT /api/icons/{iconId}` | Có |
| `PUT /api/icons/{iconId}/status` | Có |
| `POST /api/ott/send-single` | Có |

**Không mã hóa:** login, refresh, logout, me, reset-password, register, roles, GET/DELETE banner/icon, `GET /api/crypto/public-key`.

**Bắt buộc đã đăng nhập** trước khi gọi API mã hóa (AAD lấy `userId`, `jti` từ JWT).

### 2.2. Bật/tắt raw JSON (không mã hóa)

**Phía BE** (`appsettings.json`):

```json
"Runtime": {
  "EnvironmentName": "UAT"
},
"Crypto": {
  "Bypass": {
    "EnableRawPayloadBypass": true,
    "AllowedEnvironment": "UAT"
  }
}
```

Bypass **chỉ bật** khi:

- `Crypto.Bypass.EnableRawPayloadBypass === true`, **và**
- `Runtime.EnvironmentName` (hoặc `ASPNETCORE_ENVIRONMENT`) **khớp** `Crypto.Bypass.AllowedEnvironment` (mặc định `UAT`).

Khi bypass active: BE nhận **body JSON thuần** (cùng schema `CreateBannerRequest`, …), không cần envelope.

**Phía BO** (`environment.crypto.enabled`):

| `crypto.enabled` (BO) | `Bypass` (BE) | Body gửi lên |
|----------------------|---------------|--------------|
| `true` | `false` | Envelope `{ kid, alg, k, d, ts, nonce }` |
| `false` | `true` (UAT) | Raw JSON payload |
| `true` | `true` | Nên dùng raw (`enabled: false`) cho đơn giản |
| `false` | `false` | Raw JSON → **BE sẽ lỗi** nếu endpoint yêu cầu envelope |

**Khuyến nghị dev:** BE `EnableRawPayloadBypass: true` + `Runtime.EnvironmentName: UAT` + BO `crypto.enabled: false`.

**Production:** BE `EnableRawPayloadBypass: false` + BO `crypto.enabled: true` + dùng `public.pem`.

### 2.3. Thuật toán

| Thành phần | Giá trị |
|------------|---------|
| `alg` | `RSA-OAEP-SHA256+A256GCM` |
| RSA | OAEP + SHA-256, encrypt metadata 60 byte |
| AES | AES-256-GCM |
| Metadata (60 byte) | `IV(12)` + `Tag(16)` + `AesKey(32)` |
| `k` | Base64(RSA ciphertext của metadata) |
| `d` | Base64(AES-GCM ciphertext của JSON payload) |
| `ts` | Unix timestamp **giây** UTC |
| `nonce` | Chuỗi unique mỗi request (UUID) |
| Replay window | 300 giây (`Crypto.ReplayWindowSeconds`) |

**AAD** (Additional Authenticated Data) — UTF-8, nối bằng `\n`:

```
{METHOD}\n{PATH}\n{ts}\n{nonce}\n{userId}\n{jti}
```

- `METHOD`: `POST`, `PUT`, … (uppercase)
- `PATH`: path request, ví dụ `/api/banners` (không query string)
- `userId`: claim `sub` hoặc `nameid` từ JWT
- `jti`: claim `jti` từ JWT

### 2.4. Envelope gửi lên (khi bật mã hóa)

`Content-Type: application/json`

```json
{
  "kid": "rsa-key-2026-01",
  "alg": "RSA-OAEP-SHA256+A256GCM",
  "k": "<base64 RSA encrypted metadata>",
  "d": "<base64 AES-GCM ciphertext>",
  "ts": 1715000000,
  "nonce": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 2.5. Lấy public key

`GET /api/crypto/public-key?kid=rsa-key-2026-01` — **Public**, không cần JWT.

Response: mảng (thường 1 phần tử):

```json
[
  {
    "kid": "rsa-key-2026-01",
    "isActive": true,
    "publicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
  }
]
```

BO có thể dùng file `public.pem` đã copy thay vì gọi API lúc khởi động (kid phải khớp BE).

### 2.6. Pseudocode mã hóa (Web Crypto API)

```typescript
async function buildEncryptedBody(
  payload: unknown,
  options: {
    publicKeyPem: string;
    kid: string;
    method: string;
    path: string;          // e.g. '/api/banners'
    accessToken: string;   // JWT
  }
): Promise<EncryptedEnvelope> {
  const plaintext = new TextEncoder().encode(JSON.stringify(payload));
  const ts = Math.floor(Date.now() / 1000);
  const nonce = crypto.randomUUID();

  const jwt = parseJwt(options.accessToken); // sub/nameid, jti
  const userId = jwt.sub ?? jwt.nameid;
  const jti = jwt.jti;
  const aadStr = [options.method.toUpperCase(), options.path, String(ts), nonce, userId, jti].join('\n');
  const aad = new TextEncoder().encode(aadStr);

  const aesKeyBytes = crypto.getRandomValues(new Uint8Array(32));
  const iv = crypto.getRandomValues(new Uint8Array(12));

  const aesKey = await crypto.subtle.importKey('raw', aesKeyBytes, 'AES-GCM', false, ['encrypt']);
  const ciphertextWithTag = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv, additionalData: aad, tagLength: 128 },
    aesKey,
    plaintext
  );
  const buf = new Uint8Array(ciphertextWithTag);
  const tagLen = 16;
  const ciphertext = buf.slice(0, buf.length - tagLen);
  const tag = buf.slice(buf.length - tagLen);

  const metadata = new Uint8Array(60);
  metadata.set(iv, 0);
  metadata.set(tag, 12);
  metadata.set(aesKeyBytes, 28);

  const rsaKey = await importRsaPublicKey(options.publicKeyPem); // SPKI from PEM
  const encryptedMetadata = await crypto.subtle.encrypt(
    { name: 'RSA-OAEP' },
    rsaKey,
    metadata
  );

  return {
    kid: options.kid,
    alg: 'RSA-OAEP-SHA256+A256GCM',
    k: btoa(String.fromCharCode(...new Uint8Array(encryptedMetadata))),
    d: btoa(String.fromCharCode(...ciphertext)),
    ts,
    nonce
  };
}
```

**Interceptor gợi ý:** Nếu `environment.crypto.enabled` → wrap body; ngược lại gửi object payload trực tiếp.

---

## 3. Danh mục mã (dropdown UI)

BE đọc từ `ContentCatalog` trong `appsettings.json`. BO có thể:

- Hard-code giống mặc định bên dưới, hoặc
- Sau này thêm API đọc catalog (chưa có — dùng config FE mirror BE).

| Nhóm | Mã dùng cho |
|------|-------------|
| **Platforms** | `ALL`, `IOS`, `ANDROID` |
| **ActionTypes** | `NONE`, `NATIVE`, `DEEPLINK`, `WEBVIEW`, `EXTERNAL_BROWSER` |
| **Banner.Status (writable)** | `DRAFT`, `ACTIVE`, `INACTIVE`, `EXPIRED` |
| **Banner.Change status** | `DRAFT`, `ACTIVE`, `INACTIVE`, `EXPIRED` |
| **Banner.RenderModes** | `FIT_WIDTH`, `CENTER_CROP`, `CONTAIN`, `COVER` |
| **Banner.AspectRatios** | `1_1`, `16_9`, `4_3`, `3_1`, `2_1`, `9_16` (+ value số) |
| **Icon.Status (writable)** | `DRAFT`, `ACTIVE`, `INACTIVE`, `EXPIRED`, `DELETED` |
| **Icon.Change status** | `DRAFT`, `ACTIVE`, `INACTIVE`, `EXPIRED` |
| **Function-codes filter** | `ACTIVE` (default), hoặc `DRAFT`, `INACTIVE`, `EXPIRED` |

---

## 4. Quyền (function codes) — menu & nút

Sau login, `user.functions[]` có `functionCode`. Ẩn/hiện menu và nút theo mã:

| functionCode | Màn hình / hành động |
|--------------|----------------------|
| `USER_MANAGEMENT` | Quản lý user, reset password, register |
| `ROLE_MANAGEMENT` | (role admin — nếu có UI) |
| `BANNER_MANAGEMENT` | Danh sách + xem banner |
| `BANNER_CREATE` | Tạo banner |
| `BANNER_UPDATE` | Sửa banner |
| `BANNER_DELETE` | Xóa banner |
| `BANNER_CHANGE_STATUS` | Đổi trạng thái banner |
| `ICON_MANAGEMENT` | Danh sách + xem icon |
| `ICON_CREATE` | Tạo icon |
| `ICON_UPDATE` | Sửa icon |
| `ICON_DELETE` | Xóa icon |
| `ICON_CHANGE_STATUS` | Đổi trạng thái icon |
| `ICON_FUNCTION_CODE_VIEW` | (tùy chọn) xem function codes |
| `OTT_SEND_SINGLE` | Gửi OTT đơn |

`functionDisplay === 1` → hiển thị trên menu (nếu build menu từ API).

---

## 5. API chi tiết cho BackOffice

### 5.1. Auth

#### `POST /api/auth/login` — Public

**Request:**

```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Response 200:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "accessToken": "eyJhbG...",
  "expiresIn": 1800,
  "user": {
    "userId": 1,
    "userName": "admin",
    "fullName": "Super Admin",
    "email": null,
    "phone": null,
    "roleCode": "SUPER_ADMIN",
    "roleName": "Super Admin",
    "functions": [
      {
        "functionId": 1,
        "functionCode": "BANNER_MANAGEMENT",
        "functionName": "Cấu hình banner",
        "functionUrl": "/banners",
        "functionDisplay": 1
      }
    ]
  }
}
```

#### `POST /api/auth/refresh-token` — Public, cookie

**Request:** không body. Cookie `refresh_token`.

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "accessToken": "eyJhbG...",
  "expiresIn": 1800
}
```

#### `POST /api/auth/logout` — Public, cookie

**Response:**

```json
{
  "errorCode": "xx",
  "errorDescription": "Đăng xuất thành công",
  "data": null
}
```

#### `GET /api/auth/me` — Bearer

**Response:** giống object `user` trong login (không có `accessToken`).

#### `POST /api/auth/reset-password` — Bearer, `USER_MANAGEMENT`

**Request:**

```json
{
  "username": "operator01"
}
```

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "username": "operator01",
  "rawPassword": "Temp@xxxx"
}
```

---

### 5.2. Users

#### `POST /api/users/register` — Bearer, `USER_MANAGEMENT`

**Request:**

```json
{
  "userName": "operator01",
  "password": "ChangeMe@123",
  "fullName": "Operator One",
  "email": "op@example.com",
  "phone": "0900000000",
  "roleCode": "OPERATOR"
}
```

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "userId": 2,
  "userName": "operator01",
  "fullName": "Operator One",
  "email": "op@example.com",
  "phone": "0900000000",
  "roleCode": "OPERATOR",
  "roleName": "Operator"
}
```

---

### 5.3. Roles

#### `GET /api/roles` — Bearer, `USER_MANAGEMENT`

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "roles": [
    { "roleId": 2, "roleCode": "ADMIN", "roleName": "Admin" },
    { "roleId": 3, "roleCode": "OPERATOR", "roleName": "Operator" }
  ]
}
```

(Không trả `SUPER_ADMIN` — chỉ role được phép gán khi tạo user.)

---

### 5.4. Banners

#### `GET /api/banners` — Bearer, `BANNER_MANAGEMENT`

**Query:** `environmentCode` (required), `groupCode`, `categoryCode`, `typeCode`, `platform`, `status`, `keyword`, `pageIndex` (default 1), `pageSize` (default 20, max 100).

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "data": {
    "items": [ { /* BannerResponse */ } ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 42,
    "totalPages": 3
  }
}
```

**BannerResponse** (mỗi item):

```json
{
  "bannerId": 1,
  "environmentCode": "UAT",
  "groupCode": "HOME",
  "categoryCode": "PROMO",
  "typeCode": "SLIDE",
  "title": "Banner title",
  "body": "Mô tả",
  "imageUrl": "https://cdn.example/banner.png",
  "buttonText": "Xem thêm",
  "actionType": "DEEPLINK",
  "functionCode": null,
  "deepLink": "app://promo/1",
  "webUrl": null,
  "mobileFunctionCodes": ["PAYMENT", "TRANSFER"],
  "aspectRatioCode": "16_9",
  "aspectRatioValue": 1.7778,
  "imageWidthPx": 1080,
  "imageHeightPx": 608,
  "renderMode": "FIT_WIDTH",
  "priority": 10,
  "status": "ACTIVE",
  "startTime": "2026-01-01T00:00:00Z",
  "endTime": null,
  "appVersion": "2.0.0",
  "platform": "ALL",
  "loginRequired": false,
  "trackingCode": "BNR_20260101120000_1234",
  "createdAt": "2026-01-01T12:00:00Z",
  "updatedAt": null
}
```

#### `GET /api/banners/{id}` — Bearer, `BANNER_MANAGEMENT`

**Response:** `data` = một `BannerResponse`.

#### `POST /api/banners` — Bearer, `BANNER_CREATE`, body mã hóa (hoặc raw nếu bypass)

**Request payload** (object trước khi mã hóa):

```json
{
  "environmentCode": "UAT",
  "groupCode": "HOME",
  "categoryCode": "PROMO",
  "typeCode": "SLIDE",
  "title": "Banner mới",
  "body": null,
  "imageUrl": "https://cdn.example/b.png",
  "buttonText": "Chi tiết",
  "actionType": "WEBVIEW",
  "functionCode": null,
  "deepLink": null,
  "webUrl": "https://example.com",
  "mobileFunctionCodes": ["PAYMENT"],
  "aspectRatioCode": "16_9",
  "aspectRatioValue": null,
  "imageWidthPx": 1080,
  "imageHeightPx": 608,
  "renderMode": "CONTAIN",
  "priority": 0,
  "status": "DRAFT",
  "startTime": null,
  "endTime": null,
  "appVersion": "1.0.0",
  "platform": "ALL",
  "loginRequired": true
}
```

**Response:** `data` = `BannerResponse` (có `bannerId`, `trackingCode` auto).

#### `PUT /api/banners/{id}` — Bearer, `BANNER_UPDATE`

Cùng schema `CreateBannerRequest` / `UpdateBannerRequest` (full replace fields).

#### `PUT /api/banners/{id}/status` — Bearer, `BANNER_CHANGE_STATUS`

**Payload:**

```json
{
  "status": "ACTIVE"
}
```

**Response:** `data: null`.

#### `DELETE /api/banners/{id}` — Bearer, `BANNER_DELETE`

Xóa mềm (`status` → `DELETED`). **Response:** `data: null`.

**Tính năng UI banner:**

- Danh sách có filter + phân trang theo môi trường.
- Form tạo/sửa: chọn `mobileFunctionCodes` từ API function-codes của icon.
- `appVersion`: phiên bản app **tối thiểu** để hiển thị trên mobile (mobile lọc client `appVersion` ≥ giá trị này).
- Lịch `startTime` / `endTime`, `platform`, `loginRequired`.

---

### 5.5. Icons

#### `GET /api/icons` — Bearer, `ICON_MANAGEMENT`

**Query:** giống banner (`environmentCode` required, …).

**Response:** `data` = `PagedResult<IconResponse>`.

**IconResponse:**

```json
{
  "iconId": 1,
  "environmentCode": "UAT",
  "groupCode": "HOME_GRID",
  "categoryCode": "MAIN",
  "typeCode": null,
  "title": "Chuyển tiền",
  "subtitle": null,
  "iconUrl": "https://cdn.example/icon.png",
  "iconSelectedUrl": null,
  "iconDisabledUrl": null,
  "backgroundColor": "#FFFFFF",
  "textColor": "#000000",
  "functionCode": "TRANSFER",
  "deepLink": "app://transfer",
  "webUrl": null,
  "actionType": "NATIVE",
  "badgeType": "DOT",
  "badgeText": "Mới",
  "badgeColor": "#FFF",
  "badgeBgColor": "#F00",
  "badgeStartTime": null,
  "badgeEndTime": null,
  "priority": 1,
  "gridSpan": 1,
  "status": "ACTIVE",
  "startTime": null,
  "endTime": null,
  "appVersion": "2.0.0",
  "platform": "ALL",
  "loginRequired": false,
  "trackingCode": "ICO_...",
  "createdAt": "2026-01-01T12:00:00Z",
  "updatedAt": null
}
```

#### `GET /api/icons/function-codes` — Bearer, `ICON_MANAGEMENT`

**Query:** `environmentCode` (required), `groupCode`, `categoryCode`, `status` (optional, default `ACTIVE`), `keyword`.

**Response:**

```json
{
  "errorCode": "00",
  "errorDescription": "Thành công",
  "data": [
    {
      "functionCode": "TRANSFER",
      "displayName": "Chuyển tiền",
      "groupCode": "HOME_GRID",
      "categoryCode": "MAIN",
      "typeCode": null
    }
  ]
}
```

Dùng cho multi-select `mobileFunctionCodes` khi cấu hình banner.

#### `POST /api/icons` — Bearer, `ICON_CREATE`, body mã hóa/raw

**Request (payload trước mã hóa):**

```json
{
  "environmentCode": "UAT",
  "groupCode": "HOME_GRID",
  "categoryCode": "MAIN",
  "typeCode": null,
  "title": "Chuyển tiền",
  "subtitle": null,
  "iconUrl": "https://cdn.example/i.png",
  "iconSelectedUrl": null,
  "iconDisabledUrl": null,
  "backgroundColor": null,
  "textColor": null,
  "functionCode": "TRANSFER",
  "deepLink": null,
  "webUrl": null,
  "actionType": "NATIVE",
  "badgeType": null,
  "badgeText": null,
  "badgeColor": null,
  "badgeBgColor": null,
  "badgeStartTime": null,
  "badgeEndTime": null,
  "priority": 0,
  "gridSpan": 1,
  "status": "DRAFT",
  "startTime": null,
  "endTime": null,
  "appVersion": null,
  "platform": "ALL",
  "loginRequired": true
}
```

#### `PUT /api/icons/{iconId}` — `ICON_UPDATE`

#### `PUT /api/icons/{iconId}/status` — `ICON_CHANGE_STATUS`

```json
{ "status": "ACTIVE" }
```

#### `DELETE /api/icons/{iconId}` — `ICON_DELETE`

**Tính năng UI icon:**

- CRUD icon theo môi trường; `functionCode` unique trong logic nghiệp vụ (dùng cho banner).
- `categoryCode` + `groupCode`: mobile bootstrap gom nhóm category từ icon active (không có API category riêng).
- Badge có thời hạn `badgeStartTime` / `badgeEndTime`.

---

### 5.6. OTT (tùy module)

#### `POST /api/ott/send-single` — Bearer, `OTT_SEND_SINGLE`, body mã hóa/raw

**Payload** (ví dụ):

```json
{
  "receiver": "84901234567",
  "content": "Noi dung tin OTT"
}
```

(Hiện stub — response `data` chứa `receiver` đã giải mã.)

---

## 6. Gợi ý cấu trúc module Angular (cho Cursor BO)

```
src/app/
  core/
    auth/           # login, refresh, guard, interceptor JWT
    api/
      api-client.service.ts
      crypto.interceptor.ts   # wrap body nếu crypto.enabled
      crypto.service.ts       # build envelope
  features/
    banners/        # list, form, environment selector
    icons/
    users/
  shared/
    models/         # TypeScript interfaces mirror DTO
```

**Luồng chính:**

1. Login → lưu `accessToken` + `user` (sessionStorage/memory).
2. HTTP interceptor: gắn Bearer; `withCredentials: true`.
3. Trước POST/PUT banner|icon: nếu `crypto.enabled` → `buildEncryptedBody`.
4. Màn hình banner: load `function-codes` khi mở form; validate dropdown từ `ContentCatalog` (config FE).
5. Global `environmentCode` store (service) → mọi API banner/icon truyền cùng môi trường.

---

## 7. Checklist tích hợp

- [ ] Copy `public.pem`, cấu hình `kid` khớp BE (`rsa-key-2026-01`).
- [ ] Dev: thống nhất BE bypass UAT + BO `crypto.enabled: false`.
- [ ] Prod: BE bypass off + BO `crypto.enabled: true`.
- [ ] `withCredentials: true` cho refresh/logout.
- [ ] Interceptor mã hóa đúng `path` trong AAD (không có query).
- [ ] Parse JWT lấy `sub`/`nameid` và `jti` cho AAD.
- [ ] Kiểm tra `errorCode` trên mọi response.
- [ ] Ẩn PROD trên UI nếu không phải SUPER_ADMIN.

---

## 8. Tham chiếu nhanh endpoint (chỉ BO)

| Method | Path | Auth | Mã hóa body |
|--------|------|------|-------------|
| POST | `/api/auth/login` | Public | Không |
| POST | `/api/auth/refresh-token` | Cookie | Không |
| POST | `/api/auth/logout` | Cookie | Không |
| GET | `/api/auth/me` | Bearer | — |
| POST | `/api/auth/reset-password` | Bearer | Không |
| POST | `/api/users/register` | Bearer | Không |
| GET | `/api/roles` | Bearer | — |
| GET | `/api/crypto/public-key` | Public | — |
| GET | `/api/banners` | Bearer | — |
| GET | `/api/banners/{id}` | Bearer | — |
| POST | `/api/banners` | Bearer | **Có** |
| PUT | `/api/banners/{id}` | Bearer | **Có** |
| PUT | `/api/banners/{id}/status` | Bearer | **Có** |
| DELETE | `/api/banners/{id}` | Bearer | — |
| GET | `/api/icons` | Bearer | — |
| GET | `/api/icons/{id}` | Bearer | — |
| GET | `/api/icons/function-codes` | Bearer | — |
| POST | `/api/icons` | Bearer | **Có** |
| PUT | `/api/icons/{id}` | Bearer | **Có** |
| PUT | `/api/icons/{id}/status` | Bearer | **Có** |
| DELETE | `/api/icons/{id}` | Bearer | — |
| POST | `/api/ott/send-single` | Bearer | **Có** |

API `/api/mobile/config/*` dành cho **app mobile**, không bắt buộc cho BO web.

---

*Tài liệu đồng bộ với codebase DigiMerchantBE. Cập nhật lần cuối: 2026-05.*
