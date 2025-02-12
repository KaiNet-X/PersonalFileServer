namespace Common;

using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class Crypto
{
    private static Aes _aes = GetAes();

    public const ushort KeyLength = 256;
    public const ushort IvLength = 128;

    public static byte[] GenerateRandomKey(ushort keyLength) =>
        RandomNumberGenerator.GetBytes(keyLength);

    public static byte[] Hash(byte[] input)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
            return algorithm.ComputeHash(input);
    }

    public static byte[] Hash(string input) =>
        Hash(Encoding.UTF8.GetBytes(input));

    public static byte[] KeyFromHash(byte[] hash, int length = 16)
    {
        byte[] key = new byte[length];

        for (int i = 0; i < length; i++)
            key[i] = hash[i % hash.Length];

        return key;
    }

    public static async Task<byte[]> EncryptAESAsync(byte[] input, byte[] key)
    {
        var iv = Guid.NewGuid().ToByteArray()[..16];
        
        await using MemoryStream ms2 = new();
        await ms2.WriteAsync(iv);

        await using (MemoryStream memoryStream = new MemoryStream())
        {
            await using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(input, 0, input.Length);
                await cryptoStream.FlushFinalBlockAsync();
            }

            await memoryStream.CopyToAsync(ms2);
        }
        return ms2.ToArray();
    }
    
    // public static async Task<byte[]> EncryptAESAsync(Stream input, byte[] key, byte[] iv)
    // {
    //     await using (MemoryStream memoryStream = new MemoryStream())
    //     {
    //         await using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
    //         {
    //             await input.CopyToAsync(cryptoStream);
    //         }
    //         return memoryStream.ToArray();
    //     }
    // }

    // public static async Task<byte[]> DecryptAESAsync(byte[] input, byte[] key, byte[] iv)
    // {
    //     await using (MemoryStream memoryStream = new MemoryStream(input))
    //     {
    //         await using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
    //         {
    //             await using (MemoryStream outputStream = new MemoryStream())
    //             {
    //                 await cryptoStream.CopyToAsync(outputStream);
    //                 return outputStream.ToArray();
    //             }
    //         }
    //     }
    // }
    
    public static async Task<byte[]> DecryptAESAsync(Stream input, byte[] key)
    {
        var iv = new byte[16];
        await input.ReadAsync(iv, 0, 16);
        await using (CryptoStream cryptoStream = new CryptoStream(input, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
        {
            await using (MemoryStream outputStream = new MemoryStream())
            {
                await cryptoStream.CopyToAsync(outputStream);
                return outputStream.ToArray();
            }
        }
    }

    // public static async Task EncryptStreamAsync(Stream source, Stream destination, byte[] key, byte[] iv)
    // {
    //     await using CryptoStream cryptoStream = new CryptoStream(destination, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
    //     await source.CopyToAsync(cryptoStream);
    // }
    //
    // public static async Task DecryptStreamAsync(Stream source, Stream destination, byte[] key, byte[] iv)
    // {
    //     await using CryptoStream cryptoStream = new CryptoStream(source, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
    //     await cryptoStream.CopyToAsync(destination);
    // }

    public static async Task<byte[]> CompressAsync(Stream source)
    {
        await using (var memoryStream = new MemoryStream())
        {
            await using (var compression = new DeflateStream(memoryStream, CompressionLevel.SmallestSize))
            {
                await source.CopyToAsync(compression);
            }
            return memoryStream.ToArray();
        }
    }


    public static async Task<byte[]> DecompressAsync(Stream source)
    {
        await using (var memoryStream = new MemoryStream())
        {
            await using (var compression = new DeflateStream(source, CompressionMode.Decompress))
            {
                await compression.CopyToAsync(memoryStream);
            }
            return memoryStream.ToArray();
        }
    }

    public static async Task<byte[]> DecompressAsync(byte[] source)
    {
        await using (var memoryStream = new MemoryStream())
        {
            await using (var sourceStream = new MemoryStream(source))
            {
                await using (var compression = new DeflateStream(sourceStream, CompressionMode.Decompress))
                {
                    await compression.CopyToAsync(memoryStream);
                }
            }
            return memoryStream.ToArray();
        }
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