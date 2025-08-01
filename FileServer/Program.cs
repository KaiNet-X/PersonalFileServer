using System.Net;
using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using ConnectionState = FileServer.ConnectionState;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

Console.Clear();

const int PORT = 6969;
var workingDirectory = Directory.GetCurrentDirectory();
if (args.Length > 0)
    workingDirectory = args[0];

var authService = new AuthService(workingDirectory);

await authService.LoadUsersAsync();

var fileService = new FileService(workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

var server = new TcpServer([new IPEndPoint(IPAddress.Any, PORT), new IPEndPoint(IPAddress.IPv6Any, PORT)]
, new ServerSettings
{
    UseEncryption = true,
    ConnectionPollTimeout = 500000,
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

Console.CancelKeyPress += OnKill;
AppDomain.CurrentDomain.ProcessExit += OnKill;

foreach (var address in addresses)
    Console.WriteLine($"Hosting on {address}:{PORT}");

var exiting = false;
do
{
    var value = Console.ReadLine();
    var lower = (value ?? string.Empty).ToLower();

    if (lower == "exit")
        exiting = true;
    else if (lower == "users")
        foreach (var (uname, _) in authService.Users)
            Console.WriteLine(uname);
    else if (lower == "requests")
        foreach (var username in authService.CreateRequests.Keys)
            Console.WriteLine(username);
    else if (lower.StartsWith("approve "))
        await authService.ApproveUser(value[8..]);
    else if (lower.StartsWith("reject "))
        await authService.DenyUser(value[7..]);
    else if (lower.StartsWith("kick "))
    {
        var remaining = lower[5..];
        if (IPAddress.TryParse(remaining, out var ip))
            foreach (var connection in ConnectionState.Connections.Where(connection => connection.Client.RemoteEndpoint.Address.Equals(ip)))
                await connection.Client.CloseAsync();
        else
            foreach (var connection in ConnectionState.Connections.Where(conn => conn.User.Username == value[5..]))
                await connection.Client.CloseAsync();
    }
    else if (lower.StartsWith("delete "))
    {
        var username = value[7..];

        foreach (var connection in ConnectionState.Connections.Where(conn => conn.User.Username == username))
            await connection.Client.CloseAsync();

        await authService.RemoveUserAsync(username);
        fileService.RemoveUserDirecory(username);
    }
    else
    {
        Console.WriteLine("Unknown command. List of available commands are: ");
        Console.WriteLine("exit: terminates the server program");
        Console.WriteLine("users: lists users that are registered with the server");
        Console.WriteLine("requests: lists user creation requests");
        Console.WriteLine("approve [user]: approves a user request");
        Console.WriteLine("reject [user]: rejects a user request");
        Console.WriteLine("kick [user | ip]: kicks off all connections for a user or an ip address");
        Console.WriteLine("delete [user]: deletes a user from the server");
    }
} while (!exiting);

await server.ShutDownAsync();
return;

void OnConnect(ServerClient sc)
{
    ConnectionState.Connections.Add(new ConnectionState(sc, fileService, authService));
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (DisconnectionInfo info, ServerClient sc)
{
    var con = ConnectionState.Connections.FirstOrDefault(c => c.Client == sc);
    if (con != null)
        ConnectionState.Connections.Remove(con);
    Console.WriteLine($"{sc.LocalEndpoint} {info.Reason}");
}

void OnKill(object? sender, EventArgs e)
{
    server.ShutDown();
}