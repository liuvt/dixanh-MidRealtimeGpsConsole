using Microsoft.Extensions.Options;
using MidRealtimeGpsConsole.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Services;

public sealed class MidSignatureService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IOptions<MidApiOptions> _options;

    public MidSignatureService(IOptions<MidApiOptions> options)
    {
        _options = options;
    }

    public string CreateSignature(string method, object payload, string timestamp, string apiKey)
    {
        var opt = _options.Value;
        var payloadJson = JsonSerializer.Serialize(payload ?? new { }, JsonOptions);
        var canonical = $"{method.ToUpperInvariant()}|{payloadJson}|{timestamp}|{apiKey}";

        return opt.SignatureMode?.Trim() switch
        {
            "RsaSha256" => CreateRsaSha256(canonical, opt.PrivateKeyPemPath),
            _ => CreateHmacSha256(canonical, opt.SecretKeyHash)
        };
    }

    private static string CreateHmacSha256(string canonical, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("SecretKeyHash is required for HmacSha256 signature mode.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToBase64String(hash);
    }

    private static string CreateRsaSha256(string canonical, string? privateKeyPemPath)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPemPath) || !File.Exists(privateKeyPemPath))
            throw new InvalidOperationException("PrivateKeyPemPath is required and must exist for RsaSha256 signature mode.");

        var pem = File.ReadAllText(privateKeyPemPath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var signed = rsa.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signed);
    }
}
