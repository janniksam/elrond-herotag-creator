using System.Text;
using Erdcsharp.Domain;
using Org.BouncyCastle.Crypto.Digests;

namespace Elrond.Herotag.Creator.Web.Services;

public class HerotagSmartContractCalculator
{
    private const short ShardIdentiferLen = 2;

    private static readonly byte[] InitialDnsAddress =
    {
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
    };

    public static  Address Calculate(string username)
    {
        var hash = NameHash(username);
        var shardId = hash.Last();
        return Calculate(shardId);
    }

    private static Address Calculate(byte shardId)
    {
        var deployerPubkeyPrefix = InitialDnsAddress.Take(InitialDnsAddress.Length - ShardIdentiferLen);
        var deployerPubkey = deployerPubkeyPrefix.Concat(new byte[] { 0x00, shardId }).ToArray();

        var deployerPubkeyHex = ByteArrayToString(deployerPubkey);
        var account = new Account(Address.FromHex(deployerPubkeyHex));
        return ComputeAddress(account);
    }

    private static byte[] NameHash(string name)
    {
        var input = Encoding.UTF8.GetBytes(name);
        return ComputeKeccak256Hash(input);
    }

    private static byte[] ComputeKeccak256Hash(byte[] input)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        digest.BlockUpdate(input, 0, input.Length);
        digest.DoFinal(output, 0);
        return output;
    }

    private static Address ComputeAddress(Account account)
    {
        var ownerBytes = account.Address.PublicKey();
        var nonceBytes = LongToUInt32ByteArray(account.Nonce);
        var bytesToHash = ownerBytes.Concat(nonceBytes).ToArray();

        var ownerHash = ComputeKeccak256Hash(bytesToHash);

        var dnsAddressPart1 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 5, 0 };
        var dnsAddressPart2 = ownerHash.Skip(10).Take(20);

        var dnsAddress = dnsAddressPart1.Concat(dnsAddressPart2).Concat(new[] { ownerBytes[30], ownerBytes[31] })
            .ToArray();

        //var dnsAddress = bytes([0] * 8) +bytes([5, 0]) +address[10:30] + owner_bytes[30:]
        //ByteArray(8) { 0 } + 5 + 0 + ownerHash.slice(10 until 30) + ownerBytes[30] + ownerBytes[31]
        return Address.FromHex(ByteArrayToString(dnsAddress));
    }

    private static IEnumerable<byte> LongToUInt32ByteArray(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes); //reverse it so we get little endian.
        }

        return bytes;
    }

    private static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:X2}", b);
        return hex.ToString();
    }
}