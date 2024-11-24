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
                var phash = binSerializer.ReadString();
                User = new User(uname, phash);
                return true;
            }
            catch { }
        }
        return false;
    }

    public void SetUser(User user)
    {
        // Don't want to save the password or first hash on the system or send it to the server
        var hash2 = BitConverter.ToString( Crypto.Hash(Crypto.Hash(user.Password)));
        
        User = user with { Password = hash2 };
    }

    public async Task SaveUserAsync()
    {
        await using var file = File.Create(UserConfig);
        using var binWriter = new BinaryWriter(file);
        binWriter.Write(User.UserName);
        binWriter.Write(User.Password);
    }
    
    public void RemoveUser()
    {
        File.Delete(UserConfig);
        User = default;
    }
}
