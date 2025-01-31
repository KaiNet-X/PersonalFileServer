using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;
using ConnectionState = FileServer.ConnectionState;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

Console.Clear();

const int PORT = 6969;
var workingDirectory = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Files";

var authService = AuthService.Instance;

await authService.LoadUsersAsync();

if (!Directory.Exists(workingDirectory))
    Directory.CreateDirectory(workingDirectory);

var fileService = new FileService(authService, workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

var server = new TcpServer([new IPEndPoint(IPAddress.Any, PORT), new IPEndPoint(IPAddress.IPv6Any, PORT)]
, new ServerSettings 
{
    UseEncryption = true, 
    ConnectionPollTimeout = 5000,
    MaxClientConnections = 5,
    ClientRequiresWhitelistedTypes = true
});

server.OnClientConnected(OnConnect);

server.OnDisconnect(OnDisconnect);

server.OnObjectError((eFrame, sc) =>
{
    Console.WriteLine($"Potential malicious payload \"{eFrame.TypeName}\" by {sc.RemoteEndpoint}");
});

server.Start();

Announcer.Announce();

AppDomain.CurrentDomain.ProcessExit += OnKill;

foreach (var address in addresses)
    Console.WriteLine($"Hosting on {address}:{PORT}");

bool exiting = false;
do
{
    var value = Console.ReadLine();
    var lower = value.ToLower();
    
    if (lower == "exit")
        exiting = true;
    else if (lower == "list users")
        foreach (var (uname, _) in  authService.Users)
            Console.WriteLine(uname);
    else if (lower == "requests")
        foreach (var username in ConnectionState.CreateRequests.Keys)
            Console.WriteLine(username);
    else if (lower.StartsWith("approve "))
        await ConnectionState.ApproveUser(value[8..]);
    else if (lower.StartsWith("reject "))
        await ConnectionState.DenyUser(value[7..]);
} while (!exiting);

await server.ShutDownAsync();

void OnConnect(ServerClient sc)
{
    ConnectionState.Connections.Add(new ConnectionState(sc, fileService));
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (DisconnectionInfo info, ServerClient sc)
{
    ConnectionState.Connections.Remove(ConnectionState.Connections.FirstOrDefault(c => c.Client == sc));
    Console.WriteLine($"{sc.LocalEndpoint} {info.Reason}");
}

void OnKill(object? sender, EventArgs e)
{
    server.ShutDown();
}