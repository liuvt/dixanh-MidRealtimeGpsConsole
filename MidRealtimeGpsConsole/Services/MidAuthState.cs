namespace MidRealtimeGpsConsole.Services;

public sealed class MidAuthState
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset NextRefreshUtc { get; set; } = DateTimeOffset.MinValue;

    public bool ShouldRefresh(DateTimeOffset nowUtc)
        => string.IsNullOrWhiteSpace(AccessToken) || nowUtc >= NextRefreshUtc;
}
