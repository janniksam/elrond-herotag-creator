namespace Elrond.Herotag.Creator.Web.Services;

public interface ITransactionGenerator
{
    Task<string> GenerateRegisterHerotagUrlAsync(string herotag);
}