using Erdcsharp.Configuration;

namespace Elrond.Herotag.Creator.Web.BotWorkflows.UserState;

public interface IRegisterHerotagInputManager
{
    void SetNetwork(long userId, Network? network);
    Network? GetNetwork(long userId);
}