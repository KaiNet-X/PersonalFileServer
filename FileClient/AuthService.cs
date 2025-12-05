using System;

namespace FileClient;

using System.IO;
using System.Threading.Tasks;
using Common;

public class AuthService
{
    public static AuthService Instance { get; } = new();

    public User? User { get; private set; }
    public byte[]? EncKey { get; private set; }

    private static readonly string UserConfig = $"{Directory.GetCurrentDirectory()}/user.bin";


    private AuthService()
    {

    }

    public async Task<bool> TryLoadUserAsync()
    {
        if (!File.Exists(UserConfig)) return false;

        try
        {
            await using var file = File.OpenRead(UserConfig);
            using var binSerializer = new BinaryReader(file);
            var uname = binSerializer.ReadString();
            var encKey = Convert.FromBase64String(binSerializer.ReadString());
            var phash = Convert.FromBase64String(binSerializer.ReadString());
            User = new User(uname, phash);
            EncKey = encKey;
            return true;
        }
        catch { }
        return false;
    }

    public void SetUser(string username, string password)
    {
        // Don't want to save the password or first hash on the system or send it to the server
        EncKey = Crypto.Hash(password);

        User = new User(username, Crypto.Hash(EncKey));
    }

    public async Task SaveUserAsync()
    {
        if (User is null || EncKey is null)
            return;

        await using var file = File.Create(UserConfig);
        await using var binWriter = new BinaryWriter(file);
        binWriter.Write(User.Value.Username);
        binWriter.Write(Convert.ToBase64String(EncKey));
        binWriter.Write(Convert.ToBase64String(User.Value.Password));
    }

    public void RemoveUser()
    {
        File.Delete(UserConfig);
        User = default;
    }
}