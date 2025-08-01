using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Threading.Timer;

namespace FileServer;

public static class Announcer
{
    private static Timer eventTimer;
    private static Socket socket;

    static Announcer()
    {
        socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        socket.EnableBroadcast = true;
        eventTimer = new Timer(AnnounceAsync);
    }

    public static void Announce()
    {
        eventTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
    }

    private static void AnnounceAsync(object? obj)
    {
        try
        {
            socket.SendTo("KaiNet Server"u8.ToArray(), new IPEndPoint(IPAddress.Broadcast, 55555));
        }
        catch (Exception) { }
    }
}