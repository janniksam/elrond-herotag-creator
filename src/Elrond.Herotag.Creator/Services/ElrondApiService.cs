using System.Net;
using Erdcsharp.Configuration;
using Erdcsharp.Provider;
using Erdcsharp.Provider.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Elrond.Herotag.Creator.Web.Services;

public class ElrondApiService : IElrondApiService
{
    private const string MainnetProxy = "https://api.elrond.com";
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ElrondApiService> _logger;
    private record NetworkConfigCacheKey;

    public ElrondApiService(
        IMemoryCache memoryCache,
        ILogger<ElrondApiService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<ConfigDataDto> GetNetworkConfigAsync()
    {
        var cacheKey = new NetworkConfigCacheKey();

        if (!_memoryCache.TryGetValue(cacheKey, out ConfigDataDto networkConfig))
        {
            var client = new HttpClient();
            var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(Network.MainNet));

            networkConfig = await provider.GetNetworkConfig().ConfigureAwait(false);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            _memoryCache.Set(cacheKey, networkConfig, cacheEntryOptions);
        }

        return networkConfig;
    }

    public async Task<bool> IsHerotagAvailable(string herotag)
    {
        try
        {
            var client = new HttpClient();
            var requestUrl = $"{MainnetProxy}/usernames/{herotag}";
            var accountRaw = await client.GetStringAsync(requestUrl).ConfigureAwait(false);
            var account = JsonConvert.DeserializeObject<AccountDto>(accountRaw);
            return account == null ||
                   !account.Username.Equals($"{herotag}.elrond", StringComparison.InvariantCulture);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occured in {nameof(IsHerotagAvailable)}");
            return false;
        }
    }
}