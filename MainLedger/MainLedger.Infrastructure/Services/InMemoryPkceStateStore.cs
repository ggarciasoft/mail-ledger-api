using MainLedger.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MainLedger.Infrastructure.Services;

/// <summary>
/// In-memory implementation of PKCE state storage using MemoryCache
/// </summary>
public class InMemoryPkceStateStore : IPkceStateStore
{
    private readonly IMemoryCache _cache;

    public InMemoryPkceStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task StoreAsync(string state, string codeVerifier, TimeSpan expiration)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be null or empty", nameof(state));

        if (string.IsNullOrWhiteSpace(codeVerifier))
            throw new ArgumentException("Code verifier cannot be null or empty", nameof(codeVerifier));

        var cacheKey = GetCacheKey(state);
        _cache.Set(cacheKey, codeVerifier, expiration);

        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Task.FromResult<string?>(null);

        var cacheKey = GetCacheKey(state);
        _cache.TryGetValue<string>(cacheKey, out var codeVerifier);

        return Task.FromResult(codeVerifier);
    }

    public Task RemoveAsync(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Task.CompletedTask;

        var cacheKey = GetCacheKey(state);
        _cache.Remove(cacheKey);

        return Task.CompletedTask;
    }

    private static string GetCacheKey(string state) => $"pkce:{state}";
}
