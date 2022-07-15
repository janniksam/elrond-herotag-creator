using Erdcsharp.Configuration;
using Erdcsharp.Provider.Dtos;

namespace Elrond.Herotag.Creator.Web.Services;

public interface IElrondApiService
{
    Task<bool> IsHerotagAvailable(string herotag, Network network);
    Task<ConfigDataDto> GetNetworkConfigAsync(Network network);
}