# MID Realtime GPS Worker (.NET 9) – Workflow

## Overview
Service chạy nền (BackgroundService) trên VPS để:
1) Login vào MID và tự refresh token theo thời gian phù hợp  
2) Poll MID Realtime GPS theo chu kỳ (ví dụ 15s)  
3) Ghi “snapshot tạm” lên Google Sheet bằng cơ chế **Clear vùng data + Update lại toàn bộ list**  
4) (Tuỳ chọn) Chạy Playback theo lịch **3 lần/ngày** dựa trên danh sách IMEI đã có

---

## Prerequisites
### MID
- BaseUrl: `https://api-gw.midvietnam.net`
- `ApiKey`
- `Username` / `Password`
- `SecretKeyHash` (HmacSha256) hoặc `PrivateKeyPemPath` (RsaSha256)
- Các header bắt buộc: `x-timestamp`, `x-signature`, `x-api-key` (một số ví dụ curl dùng `api_key` => gửi cả 2 để an toàn)

### Google Sheets (Snapshot)
- Enable Google Sheets API trên Google Cloud
- Tạo **Service Account** và JSON key
- Share Google Sheet cho email service account với quyền Editor
- Có `SpreadsheetId` và `SheetName`

---

## Runtime Workflow (Realtime GPS → Google Sheet Snapshot)

### 1) Startup
- Load config từ `appsettings.json` / environment variables
- Khởi tạo HttpClient (BaseUrl + Timeout)
- Khởi tạo SignatureService (HMAC/RSA)
- Khởi tạo GoogleSheets writer (ServiceAccount)

### 2) Authentication Flow
#### 2.1 EnsureLoggedIn
- Nếu chưa có `AccessToken`:
  - `POST /api/v2/users/login`
  - Lưu `AccessToken` + `RefreshToken`
  - Tính `NextRefreshUtc`:
    - Ưu tiên đọc `exp` trong JWT để biết thời điểm hết hạn
    - Nếu không đọc được `exp` -> fallback theo `RefreshFallbackMinutes`

#### 2.2 RefreshIfNeeded (mỗi vòng lặp)
- Nếu `nowUtc >= NextRefreshUtc`:
  - Nếu có `RefreshToken` => `POST /api/v2/users/refresh-token`
  - Nếu refresh fail => fallback login lại

> Tất cả request đều ký:
> - `x-timestamp` (unix milliseconds)
> - `x-signature` theo canonical string: `METHOD|payloadJson|timestamp|apiKey`
> - `x-api-key` và `api_key` (compat)

---

## 3) Realtime Loop (PeriodicTimer)
Loop chạy theo `RealtimeIntervalSeconds`:

1) `RefreshIfNeeded()`
2) `GET /api/v2/realtime/gps`
3) Parse response `data` (dictionary of devices, mỗi device có `imei`)
4) Update Google Sheet theo cơ chế snapshot:

### 3.1 Snapshot to Google Sheet (Clear + Update)
- **Clear** vùng dữ liệu cũ (vd: `SheetName!A2:Z`)
- Build rows từ realtime list (1 device = 1 row)
- **Update** lại từ `SheetName!A2`

Gợi ý mapping cột mặc định:
| Column | Meaning |
|---|---|
| A | snapshot_utc |
| B | imei |
| C | vehicle_name |
| D | latitude |
| E | longitude |
| F | speed |
| G | time_unix |
| H | acc |
| I | status |
| J | signal_quality |
| K | driver |

---

## Optional: Playback Job (3 times/day)
Nếu bật Playback, service sẽ chạy theo lịch (Asia/Ho_Chi_Minh):
- 06:00
- 14:00
- 22:00

Workflow:
1) Lấy danh sách IMEI từ nguồn ổn định (DB hoặc cache đã sync từ realtime)
2) Với mỗi IMEI, gọi:
   `GET /api/v2/reports/playback?imei=...&start_date=...&end_date=...`
3) Parse `data.route[]` và lưu (DB/file/log)
4) Throttle concurrency để tránh rate-limit (vd: 2–5 parallel)

---

## Configuration Example (`appsettings.json`)
```json
{
  "MidApi": {
    "BaseUrl": "https://api-gw.midvietnam.net",
    "ApiKey": "YOUR_API_KEY",
    "Username": "YOUR_USERNAME",
    "Password": "YOUR_PASSWORD",
    "SignatureMode": "HmacSha256",
    "SecretKeyHash": "YOUR_SECRET_KEY_HASH",
    "PrivateKeyPemPath": "",
    "RealtimeIntervalSeconds": 15,
    "RefreshFallbackMinutes": 20,
    "RefreshSkewSeconds": 300,
    "RequestTimeoutSeconds": 30
  },
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "SheetName": "KET_QUA",
    "ServiceAccountJsonPath": "/root/mid/service-account.json",
    "ClearRange": "A2:Z"
  }
}