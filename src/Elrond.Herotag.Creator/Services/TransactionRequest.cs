using Erdcsharp.Domain;

namespace Elrond.Herotag.Creator.Web.Services;

public class TransactionRequest
{
    public TransactionRequest(string? receiver, TokenAmount value, int gasLimit, int gasPrice, string data, long? nonce = null)
    {
        Receiver = receiver;
        Value = value;
        GasLimit = gasLimit;
        GasPrice = gasPrice;
        Data = data;
        Nonce = nonce;
    }

    public string? Receiver { get; }
    public TokenAmount Value { get; }
    public int GasPrice { get; }
    public string Data { get; }
    public long? Nonce { get; }
    public int GasLimit { get; set; }
}