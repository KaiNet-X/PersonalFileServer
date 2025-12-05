using System.Collections.Concurrent;
using System.Net;
using Common;
using Net.Connection.Clients.Tcp;

namespace FileServer;

public class ConnectionState
{
    private static EventQueue eventQueue = new();

    public User User { get; private set; }
    public IPEndPoint Endpoint  => Client.RemoteEndpoint;

    private readonly AuthService _authService;

    public readonly ServerClient Client;
    private readonly FileService _fileService;

    public static List<ConnectionState> Connections { get; } = new();

    public bool Authenticated { get; private set; }

    public ConnectionState(ServerClient client, FileService fileService, AuthService authService)
    {
        Client = client;
        _fileService = fileService;
        _authService = authService;
        client.OnReceive<AuthenticationRequest>(req => eventQueue.Enqueue(() => OnAuthenticating(req)));
        client.OnReceive<UserCreateRequest>(req => eventQueue.Enqueue(() => authService.OnCreateUserRequest(req, this)));
        client.OnReceive<FileRequest>(OnFileRequestedV2);
        client.OnMessageReceived<FileRequestMessage>(req => eventQueue.Enqueue(() => OnFileRequested(req)));
    }

    private async Task OnAuthenticating(AuthenticationRequest request)
    {
        if (!await _authService.CheckUserAsync(request.Username, request.Password))
        {
            Console.WriteLine($"Authentication error on {Client.RemoteEndpoint}, name: {request.Username}");
            await Client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Failure, "Username or password is empty"));
            return;
        }

        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();
        await Client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Success, string.Empty));

        User = new User(request.Username, request.Password);
        Authenticated = true;
        Console.WriteLine($"{Client.RemoteEndpoint} authenticated as {request.Username}");
    }

    public async Task OnUserCreated(UserCreateRequest request)
    {
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();

        User = new User(request.Username, request.Password);
        Authenticated = true;
        Console.WriteLine($"{Client.RemoteEndpoint} authenticated as {request.Username}");

        await Client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Approved, "User approved"));
    }

    private async Task OnFileRequested(FileRequestMessage request)
    {
        if (!Authenticated)
        {
            Console.WriteLine($"Unauthenticated client {Endpoint} requested {request.Directory}");
            return;
        }

        await _fileService.HandleFileRequest(request, this);
    }

    private async Task OnFileRequestedV2(FileRequest request)
    {
        if (!Authenticated)
        {
            Console.WriteLine($"Unauthenticated client {Endpoint} requested {Path.GetDirectoryName(request.PathRequest)}");
            return;
        }

        await _fileService.HandleFileRequestV2(request, this);
    }
}