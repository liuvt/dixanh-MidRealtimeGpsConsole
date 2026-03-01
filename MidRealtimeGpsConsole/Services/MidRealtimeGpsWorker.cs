using Microsoft.Extensions.Options;
using MidRealtimeGpsConsole.Models;

namespace MidRealtimeGpsConsole.Services
{
    public sealed class MidRealtimeGpsWorker : BackgroundService
    {
        private readonly ILogger<MidRealtimeGpsWorker> _logger;
        private readonly MidApiClient _client;
        private readonly IOptions<MidApiOptions> _options;

        // / Google Sheets writer service
        private readonly GoogleSheetsSnapshotWriter _sheet;
        private bool _headerDone;

        public MidRealtimeGpsWorker(ILogger<MidRealtimeGpsWorker> logger, MidApiClient client, IOptions<MidApiOptions> options, GoogleSheetsSnapshotWriter sheet)
        {
            _logger = logger;
            _client = client;
            _options = options;
            _sheet = sheet;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MID realtime GPS worker started.");

            await _client.EnsureLoggedInAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.Value.RealtimeIntervalSeconds)));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _client.RefreshIfNeededAsync(stoppingToken);

                    var response = await _client.GetRealtimeGpsAsync(stoppingToken);
                    var count = response.Data?.Count ?? 0;
                    _logger.LogInformation("Realtime GPS OK. Devices: {Count}. Message: {Message}", count, response.Message);

                    // Ví dụ demo: log ra tối đa 3 thiết bị đầu tiên.
                    if (response.Data is not null)
                    {
                        foreach (var item in response.Data.Take(3))
                        {
                            var d = item.Value;
                            _logger.LogInformation(
                                "IMEI={Imei} Vehicle={Vehicle} Lat={Lat} Lng={Lng} Speed={Speed} Time={UnixTime}",
                                d.Imei,
                                d.VehicleName,
                                d.Latitude,
                                d.Longitude,
                                d.Speed,
                                d.Time);
                        }
                    }

                    // TODO: Ghi dữ liệu vào trong goolesheet
                    if (!_headerDone)
                    {
                        await _sheet.EnsureHeaderAsync(stoppingToken);
                        _headerDone = true;
                    }
                    var snapshotUtc = DateTimeOffset.UtcNow;
                    await _sheet.WriteSnapshotAsync(snapshotUtc, response.Data ?? new(), stoppingToken);
                    _logger.LogInformation("Sheet snapshot updated. Rows={Count}", response.Data?.Count ?? 0);

                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop failed. Will retry next cycle.");
                    // Nếu token lỗi/hết hạn, thử login lại ở vòng sau.
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("MID realtime GPS worker stopped.");
        }
    }
}
