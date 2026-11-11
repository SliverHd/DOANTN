using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Security;

public class JwtTokenService
{
    private const string DefaultSecretKey = "XepLichGiangDay_SecretKey_Dev_2026_MustOverrideInProduction_!@#987654321";
    private const string DefaultIssuer = "XepLichGiangDay.Api";
    private const string DefaultAudience = "XepLichGiangDay.Client";
    private const int DefaultExpireHours = 12;

    private readonly byte[] _secretKeyBytes;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expireHours;

    public JwtTokenService(IConfiguration? configuration = null)
    {
        var secretKey = configuration?["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            secretKey = DefaultSecretKey;
        }

        _secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        _issuer = configuration?["Jwt:Issuer"] ?? DefaultIssuer;
        _audience = configuration?["Jwt:Audience"] ?? DefaultAudience;
        _expireHours = int.TryParse(configuration?["Jwt:ExpireHours"], out var hours) && hours > 0
            ? hours
            : DefaultExpireHours;
    }

    public string TaoToken(NguoiDung user, int? maBoMon = null, TimeSpan? customLifetime = null)
    {
        var headerJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        });

        var now = DateTimeOffset.UtcNow;
        var exp = now.Add(customLifetime ?? TimeSpan.FromHours(_expireHours));

        var payload = new Dictionary<string, object?>
        {
            ["sub"] = user.MaNguoiDung.ToString(),
            ["unique_name"] = user.TenDangNhap,
            ["role"] = user.VaiTro,
            ["ma_giang_vien"] = user.MaGiangVien?.ToString(),
            ["ma_bo_mon"] = maBoMon?.ToString(),
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = exp.ToUnixTimeSeconds()
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signatureBytes = ComputeSignature(signingInput);
        var encodedSignature = Base64UrlEncode(signatureBytes);

        return $"{signingInput}.{encodedSignature}";
    }

    public ClaimsPrincipal? XacThucToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var signingInput = $"{parts[0]}.{parts[1]}";
        byte[] providedSignature;
        try
        {
            providedSignature = Base64UrlDecode(parts[2]);
        }
        catch
        {
            return null;
        }

        var expectedSignature = ComputeSignature(signingInput);
        if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;

            if (root.TryGetProperty("iss", out var issProp) && issProp.GetString() != _issuer)
            {
                return null;
            }

            if (root.TryGetProperty("aud", out var audProp) && audProp.GetString() != _audience)
            {
                return null;
            }

            if (!root.TryGetProperty("exp", out var expProp) || !expProp.TryGetInt64(out var expUnix))
            {
                return null;
            }

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowUnix > expUnix + 60) // Cho phep lech dong ho 60 giay
            {
                return null;
            }

            var sub = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
            var uniqueName = root.TryGetProperty("unique_name", out var nameProp) ? nameProp.GetString() : null;
            var role = root.TryGetProperty("role", out var roleProp) ? roleProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(uniqueName) || string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sub),
                new(ClaimTypes.Name, uniqueName),
                new(ClaimTypes.Role, role)
            };

            if (root.TryGetProperty("ma_giang_vien", out var gvProp) && gvProp.ValueKind == JsonValueKind.String)
            {
                var maGv = gvProp.GetString();
                if (!string.IsNullOrWhiteSpace(maGv))
                {
                    claims.Add(new Claim("ma_giang_vien", maGv));
                }
            }

            if (root.TryGetProperty("ma_bo_mon", out var bmProp) && bmProp.ValueKind == JsonValueKind.String)
            {
                var maBm = bmProp.GetString();
                if (!string.IsNullOrWhiteSpace(maBm))
                {
                    claims.Add(new Claim("ma_bo_mon", maBm));
                }
            }

            var identity = new ClaimsIdentity(claims, JwtAuthenticationHandler.SchemeName);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    private byte[] ComputeSignature(string signingInput)
    {
        using var hmac = new HMACSHA256(_secretKeyBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }
        return Convert.FromBase64String(base64);
    }
}
