using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;
using MidRealtimeGpsConsole.Models;

namespace MidRealtimeGpsConsole.Services;

public sealed class GoogleSheetsSnapshotWriter
{
    private readonly SheetsService _sheets;
    private readonly GoogleSheetsOptions _opt;

    public GoogleSheetsSnapshotWriter(IOptions<GoogleSheetsOptions> opt)
    {
        _opt = opt.Value;

        if (string.IsNullOrWhiteSpace(_opt.ServiceAccountJsonPath) || !File.Exists(_opt.ServiceAccountJsonPath))
            throw new InvalidOperationException("GoogleSheets: ServiceAccountJsonPath not found.");

        var credential = GoogleCredential
            .FromFile(_opt.ServiceAccountJsonPath)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _sheets = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MID-RealtimeGps-Snapshot"
        });
    }

    public async Task EnsureHeaderAsync(CancellationToken ct)
    {
        // Ghi header vào A1 nếu bạn muốn (idempotent: cứ overwrite luôn)
        var headerRange = $"{_opt.SheetName}!A1:K1";
        var header = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object>
                {
                    "snapshot_utc","imei","vehicle_name","lat","lng","speed","time_unix","acc","status","signal_quality","driver"
                }
            }
        };

        var upd = _sheets.Spreadsheets.Values.Update(header, _opt.SpreadsheetId, headerRange);
        upd.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await upd.ExecuteAsync();
    }

    public async Task WriteSnapshotAsync(
        DateTimeOffset snapshotUtc,
        Dictionary<string, RealtimeGpsItem> data,
        CancellationToken ct)
    {
        // 1) Clear vùng data cũ (A2:Z)
        var clear = new ClearValuesRequest();
        var clearReq = _sheets.Spreadsheets.Values.Clear(clear, _opt.SpreadsheetId, $"{_opt.SheetName}!{_opt.ClearRange}");
        await clearReq.ExecuteAsync();

        if (data.Count == 0) return;

        // 2) Build rows
        var rows = new List<IList<object>>(data.Count);
        foreach (var kv in data)
        {
            var d = kv.Value;

            rows.Add(new List<object>
            {
                snapshotUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                d.Imei ?? "",
                d.VehicleName ?? "",
                d.Latitude ?? "",
                d.Longitude ?? "",
                d.Speed ?? 0,
                d.Time ?? 0,
                d.Acc ?? 0,
                d.Status ?? 0,
                d.SignalQuality ?? 0,
                d.NameDriver ?? ""
            });
        }

        // 3) Update từ A2
        var body = new ValueRange { Values = rows };
        var updateRange = $"{_opt.SheetName}!A2";
        var updateReq = _sheets.Spreadsheets.Values.Update(body, _opt.SpreadsheetId, updateRange);
        updateReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await updateReq.ExecuteAsync();
    }
}