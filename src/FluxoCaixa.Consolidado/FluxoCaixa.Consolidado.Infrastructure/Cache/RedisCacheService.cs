using FluxoCaixa.Consolidado.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.Infrastructure.Cache;

public class RedisCacheService(
    IDistributedCache cache,
    IConnectionMultiplexer redis,
    ILogger<RedisCacheService> logger
) : ICacheService
{
    private static readonly JsonSerializerOptions OpcoesJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var valor = await cache.GetStringAsync(key, ct);
            return valor is null ? null : JsonSerializer.Deserialize<T>(valor, OpcoesJson);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao ler cache para chave {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        try
        {
            var serializado = JsonSerializer.Serialize(value, OpcoesJson);
            await cache.SetStringAsync(key, serializado, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao gravar cache para chave {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Falha ao remover cache para chave {Key}", key); }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var servidor = redis.GetServer(redis.GetEndPoints().First());
            var chaves = servidor.Keys(pattern: pattern).ToArray();
            if (chaves.Length > 0)
                await redis.GetDatabase().KeyDeleteAsync(chaves);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover cache por padrão {Pattern}", pattern);
        }
    }
}
