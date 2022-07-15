using Erdcsharp.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Elrond.Herotag.Creator.Web.BotWorkflows.UserState;

public class RegisterHerotagInputManager : IRegisterHerotagInputManager
{
    private readonly IMemoryCache _memoryCache;

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record HerotagUserCacheKey(long UserId);

    public RegisterHerotagInputManager(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void SetNetwork(long userId, Network? network)
    {
        var cacheKey = new HerotagUserCacheKey(userId);
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        _memoryCache.Set(cacheKey, network, cacheEntryOptions);
    }

    public Network? GetNetwork(long userId)
    {
        var cacheKey = new HerotagUserCacheKey(userId);
        _memoryCache.TryGetValue(cacheKey, out Network? network);
        return network;
    }
}