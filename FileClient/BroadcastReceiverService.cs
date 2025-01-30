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
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    public void StartReceive()
    {
        socket.Bind(new IPEndPoint(IPAddress.Any, 55555));
        socket.EnableBroadcast = true;
        cts = new CancellationTokenSource();
        Task.Factory.StartNew(async () => await ReceiveAsync(cts.Token), cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void StopReceive()
    {
        cts.Cancel();
        cts.Dispose();
    }
    
    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        var receiveBuffer = new byte[13];
        while (!cts.Token.IsCancellationRequested)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var result = await socket.ReceiveFromAsync(receiveBuffer.AsMemory(), remoteEP, cancellationToken);
            
            if (Encoding.UTF8.GetString(receiveBuffer) == "KaiNet Server" && result.RemoteEndPoint is IPEndPoint ep)
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
        cts.Cancel();
        cts.Dispose();
    }
}