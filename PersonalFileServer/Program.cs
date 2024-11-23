using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;
using ConnectionState = FileServer.ConnectionState;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

const int PORT = 6969;
var workingDirectory = @$"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Files";

var authService = new AuthService();
await authService.LoadUsersAsync();

if (!Directory.Exists(workingDirectory))
    Directory.CreateDirectory(workingDirectory);

var fileService = new FileService(authService, workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

var server = new TcpServer([new IPEndPoint(IPAddress.Any, PORT), new IPEndPoint(IPAddress.IPv6Any, PORT)]
, new ServerSettings 
{
    UseEncryption = true, 
    ConnectionPollTimeout = 100000,
    MaxClientConnections = 5,
    ClientRequiresWhitelistedTypes = true
});


var connections = new List<ConnectionState>();

server.OnClientConnected(OnConnect);

server.OnDisconnect(OnDisconnect);

server.OnObjectError((eFrame, sc) =>
{
    Console.WriteLine($"Potential malicious payload \"{eFrame.TypeName}\" by {sc.RemoteEndpoint}");
});

server.Start();

AppDomain.CurrentDomain.ProcessExit += OnKill;

foreach (var address in addresses)
    Console.WriteLine($"Hosting on {address}:{PORT}");

bool exiting = false;
do
{
    var value = Console.ReadLine()?.ToUpper();
    switch (value)
    {
        case "EXIT":
            exiting = true;
            break;
        case "LIST USERS":
            foreach (var (uname, _) in  authService.Users)
            {
                Console.WriteLine(uname);
            }
        break;
        default:
            Console.WriteLine("Unknown command");
            break;
    }
} while (!exiting);

await server.ShutDownAsync();

void OnConnect(ServerClient sc)
{
    connections.Add(new ConnectionState(sc, authService, fileService));
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (DisconnectionInfo info, ServerClient sc)
{
    connections.Remove(connections.FirstOrDefault(c => c.Client == sc));
    Console.WriteLine($"{sc.LocalEndpoint} {info.Reason}");
}

void OnKill(object? sender, EventArgs e)
{
    server.ShutDown();
}