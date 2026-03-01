using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using MidRealtimeGpsConsole.Models;
using MidRealtimeGpsConsole.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MidApiOptions>(builder.Configuration.GetSection("MidApi"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<MidSignatureService>();
builder.Services.AddSingleton<MidAuthState>();
builder.Services.AddHttpClient<MidApiClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<MidApiOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/'));
    http.Timeout = TimeSpan.FromSeconds(opt.RequestTimeoutSeconds <= 0 ? 30 : opt.RequestTimeoutSeconds);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHostedService<MidRealtimeGpsWorker>();

//Googlesheet service   
builder.Services.Configure<GoogleSheetsOptions>(builder.Configuration.GetSection("GoogleSheets"));
builder.Services.AddSingleton<GoogleSheetsSnapshotWriter>();

await builder.Build().RunAsync();


