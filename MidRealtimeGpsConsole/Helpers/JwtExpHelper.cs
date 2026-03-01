using System.Text;
using System.Text.Json;

namespace MidRealtimeGpsConsole.Helpers;
public static class JwtExpHelper
{
    public static DateTimeOffset ResolveRefreshTime(
        string jwt,
        DateTimeOffset nowUtc,
        TimeSpan fallbackLifetime,
        TimeSpan skew)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length >= 2)
            {
                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expElement) && expElement.TryGetInt64(out var expUnix))
                {
                    var expUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                    var refreshAt = expUtc - skew;
                    return refreshAt > nowUtc ? refreshAt : nowUtc.AddMinutes(1);
                }
            }
        }
        catch
        {
            // ignore and fallback
        }

        return nowUtc.Add(fallbackLifetime);
    }
}
