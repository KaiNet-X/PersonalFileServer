namespace FileClient;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class BroadcastReceiverService : IDisposable
{
    private Socket socket;
    private CancellationTokenSource cts;
    
    public Action<string> OnServer;
    public HashSet<IPAddress> ServerAddresses { get; } = new ();
    
    public static BroadcastReceiverService Instance { get; } = new();
    
    private BroadcastReceiverService()
    {
        socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
    }

    public void StartReceive()
    {
        socket.Bind(new IPEndPoint(IPAddress.Any, 19995));
        socket.EnableBroadcast = true;
        cts = new CancellationTokenSource();
        Task.Factory.StartNew(Receive, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void StopReceive()
    {
        cts.Cancel();
        cts.Dispose();
    }
    
    private void Receive()
    {
        var receiveBuffer = new byte[13];
        while (!cts.Token.IsCancellationRequested)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            socket.ReceiveFrom(receiveBuffer, ref remoteEP);
            if (Encoding.UTF8.GetString(receiveBuffer) == "KaiNet Server" && remoteEP is IPEndPoint ep)
            {
                if (!ServerAddresses.Add(ep.Address))
                    return;
                OnServer?.Invoke(ep.Address.MapToIPv4().ToString());
            }
        }
    }

    public void Dispose()
    {
        socket.Close();
        cts.Dispose();
    }
}