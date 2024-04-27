using Common;

namespace WinformsFileClient;

internal static class Auth
{
    public static User User { get; private set; }
    private static readonly string UserConfig = $"{Directory.GetCurrentDirectory()}/user.bin";
    public static async Task<bool> TryLoadUser()
    {
        if (File.Exists("user.bin"))
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

    public static async Task SetUser(User user)
    {
        User = user;
        await using var file = File.Create(UserConfig);
        using var binWriter = new BinaryWriter(file);
        binWriter.Write(user.UserName);
        binWriter.Write(user.Password);
    }
}
