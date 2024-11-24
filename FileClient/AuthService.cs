using System;

namespace FileClient;

using Common;
using System.IO;
using System.Threading.Tasks;

public class AuthService
{
    private static readonly AuthService _instance = new AuthService();
    public static AuthService Instance => _instance;

    public User User { get; private set; }
    public byte[] EncKey { get; private set; }
    public byte[] IV { get; private set; }
    
    private static readonly string UserConfig = $"{Directory.GetCurrentDirectory()}/user.bin";


    private AuthService()
    {
        
    }

    public async Task<bool> TryLoadUserAsync()
    {
        if (File.Exists(UserConfig))
        {
            try
            {
                await using var file = File.OpenRead(UserConfig);
                using var binSerializer = new BinaryReader(file);
                var uname = binSerializer.ReadString();
                var encKey = Convert.FromBase64String(binSerializer.ReadString());
                var phash = Convert.FromBase64String(binSerializer.ReadString());
                User = new User(uname, phash);
                EncKey = encKey;
                IV = EncKey[..16];
                return true;
            }
            catch { }
        }
        return false;
    }

    public void SetUser(string username, string password)
    {
        // Don't want to save the password or first hash on the system or send it to the server
        EncKey = Crypto.Hash(password);
        IV = EncKey[..16];
        
        User = new User(username, Crypto.Hash(EncKey));
    }

    public async Task SaveUserAsync()
    {
        await using var file = File.Create(UserConfig);
        using var binWriter = new BinaryWriter(file);
        binWriter.Write(User.Username);
        binWriter.Write(Convert.ToBase64String(EncKey));
        binWriter.Write(Convert.ToBase64String(User.Password));
    }
    
    public void RemoveUser()
    {
        File.Delete(UserConfig);
        User = default;
    }
}
