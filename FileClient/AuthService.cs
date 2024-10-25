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

    public async Task SetUser(User user)
    {
        User = user;
        await using var file = File.Create(UserConfig);
        using var binWriter = new BinaryWriter(file);
        binWriter.Write(user.UserName);
        binWriter.Write(user.Password);
    }
}
