using System.Text;
using System.Text.Encodings.Web;
using Elrond.Herotag.Creator.Web.Extensions;
using Erdcsharp.Domain;
using Erdcsharp.Provider.Dtos;

namespace Elrond.Herotag.Creator.Web.Services;

public class TransactionGenerator : ITransactionGenerator
{
    private const string MainnetWalletUrl = "https://wallet.elrond.com";
    private readonly IElrondApiService _elrondApiService;

    public TransactionGenerator(IElrondApiService elrondApiService)
    {
        _elrondApiService = elrondApiService;
    }

    public async Task<string> GenerateRegisterHerotagUrlAsync(string herotag)
    {
        var herotagWithSuffix = $"{herotag}.elrond";
        var receiver = HerotagSmartContractCalculator.Calculate(herotagWithSuffix);
        var networkConfig = await _elrondApiService.GetNetworkConfigAsync();
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
        return GetTransactionUrl(request);
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

    public string GetTransactionUrl(TransactionRequest request, string? callbackUrl = null)
    {
        return $"{MainnetWalletUrl}/hook/transaction/?{BuildTransactionUrl(request, callbackUrl)}";
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