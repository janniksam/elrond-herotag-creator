using System.Text;
using System.Text.Encodings.Web;
using Elrond.Herotag.Creator.Web.Extensions;
using Erdcsharp.Configuration;
using Erdcsharp.Domain;
using Erdcsharp.Provider.Dtos;

namespace Elrond.Herotag.Creator.Web.Services;

public class TransactionGenerator : ITransactionGenerator
{
    private readonly IElrondApiService _elrondApiService;

    public TransactionGenerator(IElrondApiService elrondApiService)
    {
        _elrondApiService = elrondApiService;
    }

    public async Task<string> GenerateRegisterHerotagUrlAsync(string herotag, Network network)
    {
        var herotagWithSuffix = $"{herotag}.elrond";
        var receiver = HerotagSmartContractCalculator.Calculate(herotagWithSuffix);
        var networkConfig = await _elrondApiService.GetNetworkConfigAsync(network);
        var minGasLimit = networkConfig.Config.erd_min_gas_limit;
        var minGasPrice = networkConfig.Config.erd_min_gas_price;

        var data = $"register@{herotagWithSuffix.ToHex().ToUpper()}";
        var request = new TransactionRequest(
            receiver.Bech32,
            TokenAmount.Zero(),
            minGasLimit,
            minGasPrice,
            data);
            
        request.GasLimit = CalculateGasPrice(request, networkConfig, 12000000);
        return GetTransactionUrl(request, network);
    }

    private static int CalculateGasPrice(
        TransactionRequest request,
        ConfigDataDto configDataDto,
        int additionalGas)
    {
        var value = configDataDto.Config.erd_min_gas_limit + additionalGas;
        if (string.IsNullOrEmpty(request.Data))
            return value;

        var bytes = Encoding.UTF8.GetBytes(request.Data);
        value += configDataDto.Config.erd_gas_per_data_byte * bytes.Length;

        return value;
    }

    public string GetTransactionUrl(TransactionRequest request, Network network, string? callbackUrl = null)
{
        return $"{GetWalletUrl(network)}/hook/transaction/?{BuildTransactionUrl(request, callbackUrl)}";
    }

    private string GetWalletUrl(Network network)
    {
        return network switch
        {
            Network.MainNet => "https://wallet.elrond.com",
            Network.TestNet => "https://testnet-wallet.elrond.com",
            Network.DevNet => "https://devnet-wallet.elrond.com",
            _ => throw new ArgumentOutOfRangeException(nameof(network), network, null)
        };
    } 


    private static string BuildTransactionUrl(TransactionRequest request, string? callbackUrl)
    {
        StringBuilder builder = new();
        builder.Append($"value={request.Value.Value}");
        builder.Append($"&gasLimit={request.GasLimit}");
        builder.Append($"&gasPrice={request.GasPrice}");
        builder.Append($"&data={request.Data}");

        if (request.Receiver != null)
        {
            builder.Append($"&receiver={request.Receiver}");
        }

        if (request.Nonce != null)
        {
            builder.Append($"&nonce={request.Nonce}");
        }

        if (callbackUrl != null)
        {
            var callbackUrlEncoded = UrlEncoder.Default.Encode(callbackUrl);
            builder.Append($"&callbackUrl={callbackUrlEncoded}");
        }

        return builder.ToString();
    }
}