# MidRealtimeGpsConsole

Console app .NET 9 chạy nền để:
- login MID
- tự refresh token trước khi hết hạn
- gọi API realtime GPS theo chu kỳ

## Chạy local / VPS
1. Cài .NET 9 SDK/runtime
2. Sửa `appsettings.json`
3. Chạy:
   - `dotnet restore`
   - `dotnet run`

## Publish Linux VPS
```bash
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

## systemd (Ubuntu)
Tạo `/etc/systemd/system/mid-realtime-gps.service`

```ini
[Unit]
Description=MID Realtime GPS Worker
After=network.target

[Service]
WorkingDirectory=/opt/mid-realtime-gps
ExecStart=/usr/bin/dotnet /opt/mid-realtime-gps/MidRealtimeGpsConsole.dll
Restart=always
RestartSec=5
User=root
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Sau đó:
```bash
sudo systemctl daemon-reload
sudo systemctl enable mid-realtime-gps
sudo systemctl start mid-realtime-gps
sudo systemctl status mid-realtime-gps
```

## Lưu ý chữ ký
Tài liệu MID mô tả phần chữ ký chưa hoàn toàn rõ ràng. Project đang hỗ trợ 2 mode:
- `HmacSha256`
- `RsaSha256`

Nếu MID yêu cầu thuật toán khác (ví dụ HMAC rồi mới RSA trên chuỗi/hash), chỉ cần chỉnh trong `MidSignatureService`.
