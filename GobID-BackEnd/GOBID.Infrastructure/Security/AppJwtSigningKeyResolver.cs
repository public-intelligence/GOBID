using Dapper;
using GOBID.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace GOBID.Infrastructure.Security;

/// <summary>
/// Resuelve la clave de firma para tokens JWT M2M.
/// Si el kid tiene formato "app:{applicationId}", deriva la clave por aplicación.
/// De lo contrario retorna la clave estática global (Jwt:AppKey).
/// </summary>
/// <remarks>
/// Las claves derivadas se cachean con vencimiento. El vencimiento no es una optimización: la
/// clave depende de Jwt:Key y del random_string de la aplicación, así que una caché perpetua deja
/// la validación firmando contra una clave retirada hasta que el proceso reinicie. Eso es
/// exactamente lo que alarga una rotación de clave maestra. Se ajusta con
/// Jwt:AppKeyCacheMinutes (5 minutos por omisión).
/// </remarks>
public class AppJwtSigningKeyResolver
{
    private const string AppKidPrefix = "app:";
    private const int DefaultCacheMinutes = 5;

    private readonly IConfiguration _config;
    private readonly string _connectionString;
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<Guid, CachedKey> _cache = new();

    public AppJwtSigningKeyResolver(IConfiguration config)
    {
        _config = config;
        _connectionString = config.GetConnectionString("default")
            ?? throw new InvalidOperationException("Connection string 'default' no configurada.");

        var minutes = config.GetValue<int?>("Jwt:AppKeyCacheMinutes") ?? DefaultCacheMinutes;
        if (minutes <= 0) minutes = DefaultCacheMinutes;
        _cacheDuration = TimeSpan.FromMinutes(minutes);
    }

    public IEnumerable<SecurityKey> ResolveKey(
        string token,
        SecurityToken securityToken,
        string kid,
        TokenValidationParameters validationParameters)
    {
        // Token M2M: kid con formato "app:{applicationId}"
        if (!string.IsNullOrEmpty(kid) && kid.StartsWith(AppKidPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var appIdStr = kid[AppKidPrefix.Length..];
            if (Guid.TryParse(appIdStr, out var applicationId))
                return [GetOrDeriveKey(applicationId)];
        }

        // Token de usuario: clave estática global
        var base64UrlSecret = _config.GetRequired("Jwt:AppKey");
        var keyBytes = Base64UrlEncoder.DecodeBytes(base64UrlSecret);
        return [new SymmetricSecurityKey(keyBytes)];
    }

    /// <summary>Descarta las claves cacheadas. Para usar tras rotar la clave maestra.</summary>
    public void InvalidateCache() => _cache.Clear();

    private SecurityKey GetOrDeriveKey(Guid applicationId)
    {
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(applicationId, out var cached) && cached.ExpiresAt > now)
            return cached.Key;

        var key = DeriveKey(applicationId);
        _cache[applicationId] = new CachedKey(key, now.Add(_cacheDuration));
        return key;
    }

    private SecurityKey DeriveKey(Guid applicationId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        var json = conn.QueryFirstOrDefault<string>(
            "read_random_string",
            new { application_id = applicationId },
            commandType: CommandType.StoredProcedure);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"No se pudo obtener el string key para la app {applicationId}.");

        var parsed = JsonConvert.DeserializeObject<RandomStringResponse>(json);
        var str = parsed?.RandomString;

        if (string.IsNullOrWhiteSpace(str))
            throw new InvalidOperationException("El resultado 'random_string' está vacío.");

        var masterKey = _config.GetRequired("Jwt:Key");

        var issuer = _config["Jwt:Issuer"] ?? string.Empty;
        var audience = _config["Jwt:Audience"] ?? string.Empty;
        var context = $"{issuer}|{audience}|app:{applicationId}|str:{str}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(masterKey));
        var keyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(context));
        return new SymmetricSecurityKey(keyBytes);
    }

    private sealed record CachedKey(SecurityKey Key, DateTimeOffset ExpiresAt);

    private sealed class RandomStringResponse
    {
        [JsonProperty("random_string")]
        public string? RandomString { get; set; }
    }
}
