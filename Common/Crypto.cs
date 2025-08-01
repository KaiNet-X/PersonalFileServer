namespace Common;

using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class Crypto
{
    private static readonly Aes Aes = GetAes();

    private const ushort KeyLength = 256;
    private const ushort IvLength = 128;

    public static byte[] GenerateRandomKey(ushort keyLength) =>
        RandomNumberGenerator.GetBytes(keyLength);

    public static byte[] Hash(byte[] input) =>
        SHA512.HashData(input);

    public static byte[] Hash(string input) =>
        Hash(Encoding.UTF8.GetBytes(input));

    public static byte[] KeyFromHash(byte[] hash, int length = 32)
    {
        var key = new byte[length];

        for (var i = 0; i < length; i++)
            key[i] = hash[i % hash.Length];

        return key;
    }

    public static async Task<byte[]> EncryptAesAsync(byte[] input, byte[] key)
    {
        var iv = Guid.NewGuid().ToByteArray()[..16];

        await using var ms2 = new MemoryStream();
        await ms2.WriteAsync(iv);

        await using (var memoryStream = new MemoryStream())
        {
            await using (var cryptoStream = new CryptoStream(memoryStream, Aes.CreateEncryptor(key[..32], iv), CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(input);
                await cryptoStream.FlushFinalBlockAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(ms2);
            }
        }
        return ms2.ToArray();
    }

    public static async Task<byte[]> DecryptAesAsync(Stream input, byte[] key)
    {
        var iv = new byte[16];
        await input.ReadAsync(iv.AsMemory(0, 16));
        await using var outputStream = new MemoryStream();
        await using (var cryptoStream = new CryptoStream(input, Aes.CreateDecryptor(key[..32], iv), CryptoStreamMode.Read))
        {
            await cryptoStream.CopyToAsync(outputStream);
        }
        return outputStream.ToArray();
    }

    public static async Task<byte[]> CompressAsync(Stream source)
    {
        await using var memoryStream = new MemoryStream();
        await using (var compression = new DeflateStream(memoryStream, CompressionLevel.SmallestSize))
        {
            await source.CopyToAsync(compression);
        }
        return memoryStream.ToArray();
    }

    public static async Task<byte[]> DecompressAsync(byte[] source)
    {
        await using var memoryStream = new MemoryStream();
        await using (var sourceStream = new MemoryStream(source))
        {
            await using (var compression = new DeflateStream(sourceStream, CompressionMode.Decompress))
            {
                await compression.CopyToAsync(memoryStream);
            }
        }
        return memoryStream.ToArray();
    }

    private static Aes GetAes()
    {
        var a = Aes.Create();
        a.Padding = PaddingMode.PKCS7;
        a.Mode = CipherMode.CBC;
        a.KeySize = KeyLength;
        a.BlockSize = IvLength;
        return a;
    }
}