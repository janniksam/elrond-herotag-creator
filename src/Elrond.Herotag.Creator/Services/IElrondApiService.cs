using Erdcsharp.Domain;
using Erdcsharp.Provider.Dtos;

namespace Elrond.Herotag.Creator.Web.Services;

public interface IElrondApiService
{
    Task<bool> IsHerotagAvailable(string herotag);
    Task<ConfigDataDto> GetNetworkConfigAsync();
}