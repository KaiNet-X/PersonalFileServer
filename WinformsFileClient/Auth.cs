using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinformsFileClient;

internal static class Auth
{
    public static User User { get; private set; }

    public static async Task<bool> TryLoadUser()
    {
        if (File.Exists("user.bin"))
        {
            try
            {
                await using var file = File.OpenRead("user.bin");
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
}
