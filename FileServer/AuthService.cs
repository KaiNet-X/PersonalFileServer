using System.Collections.Immutable;

namespace FileServer;

using Common;
using System.Collections.Generic;
using System.Text.Json;

public class AuthService
{
    private readonly EncryptionPair encryption;

    private readonly string PasswordPath;
    private readonly string EncryptionPath;
    private Dictionary<string, byte[]> users;

    public static AuthService Instance { get; } = new();
    public ImmutableDictionary<string, byte[]> Users { get; private set;  }
    
    private AuthService()
    {
        PasswordPath = $@"{Directory.GetCurrentDirectory()}/Users.json";
        EncryptionPath = $@"{Directory.GetCurrentDirectory()}/Enc.json";
        encryption = InitializeEncryption();
    }
    
    private EncryptionPair InitializeEncryption()
    {
        if (File.Exists(EncryptionPath))
        {
            using var file = File.OpenRead(EncryptionPath);

            return JsonSerializer.Deserialize<EncryptionPair>(file);
        }
        else
        {
            var enc = new EncryptionPair(Crypto.GenerateRandomKey(Crypto.KeyLength / 8), Crypto.GenerateRandomKey(Crypto.IvLength / 8));

            using var file = File.OpenWrite(EncryptionPath);

            JsonSerializer.Serialize(file, enc);

            return enc;
        }
    }

    public async Task LoadUsersAsync()
    {
        FileStream file = null;
        if (!File.Exists(PasswordPath))
        {
            file = File.Create(PasswordPath);
            file.WriteByte((byte)'{');
            file.WriteByte((byte)'}');
            await file.FlushAsync();
            file.Seek(0, SeekOrigin.Begin);
        }

        file ??= File.OpenRead(PasswordPath);

        users = await JsonSerializer.DeserializeAsync<Dictionary<string, byte[]>>(file);
        Users = users.ToImmutableDictionary();
        
        await file.DisposeAsync();
    }

    public async Task SaveUsersAsync()
    {
        using FileStream file = File.Create(PasswordPath);
        await JsonSerializer.SerializeAsync(file, users);
    }

    public async Task AddUser(string username, byte[] password)
    {
        if (users.ContainsKey(username)) return;

        var pHash = Crypto.Hash(Crypto.Hash(password));
        var encHash = await Crypto.EncryptAESAsync(pHash, encryption.Key, encryption.Iv);

        users.Add(username, encHash);
    }

    public async Task<bool> CheckUserAsync(string username, byte[] password)
    {
        if (!users.ContainsKey(username)) return false;

        var pHash = Crypto.Hash(Crypto.Hash(password));
        var encHash = await Crypto.EncryptAESAsync(pHash, encryption.Key, encryption.Iv);

        return encHash.SequenceEqual(users[username]);
    }

    public async Task<byte[]> GetUserKeyAsync(string username, byte[] password)
    {
        if (!users.ContainsKey(username)) return Array.Empty<byte>();

        var kHash = Crypto.Hash(password);
        var pHash = Crypto.Hash(kHash);
        var encHash = await Crypto.EncryptAESAsync(pHash, encryption.Key, encryption.Iv);

        if (!encHash.SequenceEqual(users[username]))
            return Array.Empty<byte>();

        return Crypto.KeyFromHash(kHash);
    }

    private record struct EncryptionPair(byte[] Key, byte[] Iv);
}
