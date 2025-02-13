using System.Linq;

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
    private readonly Socket _socket;
    private CancellationTokenSource? _cts;
    
    public Action<string>? OnServer;
    
    private readonly HashSet<IPAddress> _serverAddresses = [];
    
    public IEnumerable<string> ServerAddresses => _serverAddresses.Select(addr => addr.MapToIPv4().ToString());
    
    public static BroadcastReceiverService Instance { get; } = new();
    
    private BroadcastReceiverService()
    {
        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    public void StartReceive()
    {
        _socket.Bind(new IPEndPoint(IPAddress.Any, 55555));
        _socket.EnableBroadcast = true;
        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(async () => await ReceiveAsync(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    
    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        var receiveBuffer = new byte[13];
        while (_cts is { Token.IsCancellationRequested: false })
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            var result = await _socket.ReceiveFromAsync(receiveBuffer.AsMemory(), remoteEp, cancellationToken);
            
            if (Encoding.UTF8.GetString(receiveBuffer) == "KaiNet Server" && result.RemoteEndPoint is IPEndPoint ep)
            {
                if (!_serverAddresses.Add(ep.Address))
                    return;
                OnServer?.Invoke(ep.Address.MapToIPv4().ToString());
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _socket.Close();
        _cts?.Cancel();
        _cts?.Dispose();
    }
}