using Microsoft.Extensions.Options;
using MidRealtimeGpsConsole.Helpers;
using MidRealtimeGpsConsole.Models;
using MidRealtimeGpsConsole.Responses;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Services;

public sealed class MidApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly HttpClient _http;
    private readonly MidSignatureService _signature;
    private readonly MidAuthState _state;
    private readonly IOptions<MidApiOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MidApiClient> _logger;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public MidApiClient(
        HttpClient http,
        MidSignatureService signature,
        MidAuthState state,
        IOptions<MidApiOptions> options,
        TimeProvider timeProvider,
        ILogger<MidApiClient> logger)
    {
        _http = http;
        _signature = signature;
        _state = state;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_state.AccessToken))
            return;

        await _authLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_state.AccessToken))
                return;

            await LoginAsync(ct);
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task RefreshIfNeededAsync(CancellationToken ct)
    {
        if (!_state.ShouldRefresh(_timeProvider.GetUtcNow()))
            return;

        await _authLock.WaitAsync(ct);
        try
        {
            if (!_state.ShouldRefresh(_timeProvider.GetUtcNow()))
                return;

            if (!string.IsNullOrWhiteSpace(_state.RefreshToken))
            {
                try
                {
                    await RefreshTokenAsync(ct);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Refresh token failed. Falling back to login.");
                }
            }

            await LoginAsync(ct);
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<RealtimeGpsResponse> GetRealtimeGpsAsync(CancellationToken ct)
    {
        await EnsureLoggedInAsync(ct);

        var queryObj = new { };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/realtime/gps");
        ApplyAuthHeaders(request, HttpMethod.Get.Method, queryObj);

        using var response = await _http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            ThrowMidHttpException("Realtime GPS", response.StatusCode, content);
        }

        var model = JsonSerializer.Deserialize<RealtimeGpsResponse>(content, JsonOptions)
                    ?? throw new InvalidOperationException("Realtime GPS response is empty.");

        if (!model.Result)
        {
            ThrowMidApiException("Realtime GPS", model.Status, model.Message);
        }

        return model;
    }

    private async Task LoginAsync(CancellationToken ct)
    {
        var opt = _options.Value;
        var body = new LoginRequest(opt.Username, opt.Password);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/users/login")
        {
            Content = BuildJsonContent(body)
        };

        ApplyAuthHeaders(request, HttpMethod.Post.Method, body);

        using var response = await _http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            ThrowMidHttpException("Login", response.StatusCode, content);
        }

        var model = JsonSerializer.Deserialize<LoginResponse>(content, JsonOptions)
                    ?? throw new InvalidOperationException("Login response is empty.");

        if (!model.Result || model.Data is null || model.Data.Count == 0)
        {
            ThrowMidApiException("Login", model.Status, model.Message);
        }

        var tokenData = model.Data[0];
        var now = _timeProvider.GetUtcNow();
        _state.AccessToken = tokenData.Token;
        _state.RefreshToken = tokenData.RefreshToken;
        _state.NextRefreshUtc = JwtExpHelper.ResolveRefreshTime(
            tokenData.Token,
            now,
            TimeSpan.FromMinutes(Math.Max(1, opt.RefreshFallbackMinutes)),
            TimeSpan.FromSeconds(Math.Max(30, opt.RefreshSkewSeconds)));

        _logger.LogInformation("Login success. Next refresh planned at {NextRefreshUtc}.", _state.NextRefreshUtc);
    }

    private async Task RefreshTokenAsync(CancellationToken ct)
    {
        var opt = _options.Value;
        var body = new RefreshTokenRequest(_state.RefreshToken!);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/users/refresh-token")
        {
            Content = BuildJsonContent(body)
        };

        ApplyAuthHeaders(request, HttpMethod.Post.Method, body);

        using var response = await _http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            ThrowMidHttpException("Refresh token", response.StatusCode, content);
        }

        var model = JsonSerializer.Deserialize<RefreshTokenResponse>(content, JsonOptions)
                    ?? throw new InvalidOperationException("Refresh response is empty.");

        if (!model.Result || model.Data is null || model.Data.Count == 0)
        {
            ThrowMidApiException("Refresh token", model.Status, model.Message);
        }

        var tokenData = model.Data[0];
        var now = _timeProvider.GetUtcNow();
        _state.AccessToken = tokenData.Token;
        _state.NextRefreshUtc = JwtExpHelper.ResolveRefreshTime(
            tokenData.Token,
            now,
            TimeSpan.FromMinutes(Math.Max(1, opt.RefreshFallbackMinutes)),
            TimeSpan.FromSeconds(Math.Max(30, opt.RefreshSkewSeconds)));

        _logger.LogInformation("Refresh token success. Next refresh planned at {NextRefreshUtc}.", _state.NextRefreshUtc);
    }

    private void ApplyAuthHeaders(HttpRequestMessage request, string method, object payload)
    {
        var opt = _options.Value;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signature = _signature.CreateSignature(method, payload, timestamp, opt.ApiKey);

        // Theo phần chi tiết API, header dùng x-api-key / x-signature / x-timestamp.
        // Phần curl ở đầu tài liệu lại có chỗ ghi api_key. Nếu MID yêu cầu api_key, thêm cả 2 header.
        request.Headers.Remove("x-api-key");
        request.Headers.Remove("x-signature");
        request.Headers.Remove("x-timestamp");
        request.Headers.Remove("api_key");

        request.Headers.TryAddWithoutValidation("x-api-key", opt.ApiKey);
        request.Headers.TryAddWithoutValidation("x-signature", signature);
        request.Headers.TryAddWithoutValidation("x-timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("api_key", opt.ApiKey);

        if (!string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.AccessToken);
        }
    }

    private static StringContent BuildJsonContent<T>(T body)
        => new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    private static void ThrowMidHttpException(string action, System.Net.HttpStatusCode statusCode, string content)
        => throw new HttpRequestException($"{action} HTTP {(int)statusCode}: {content}");

    private static void ThrowMidApiException(string action, int? status, string? message)
        => throw new InvalidOperationException($"{action} API error. status={status}, message={message}");
}

