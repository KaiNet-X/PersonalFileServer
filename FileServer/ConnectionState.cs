using System.Collections.Concurrent;
using System.Net;
using Common;
using Net.Connection.Clients.Tcp;

namespace FileServer;

public class ConnectionState
{
    public static ConcurrentDictionary<string, (ConnectionState connection, UserCreateRequest request)> CreateRequests = new ();
    private static SemaphoreSlim requestsSemaphore = new(1, 1);
    
    private static EventQueue eventQueue = new();
    
    public User User { get; private set; }
    public IPEndPoint Endpoint  => Client.RemoteEndpoint;

    private static AuthService authService = AuthService.Instance;
    
    public readonly ServerClient Client;
    private readonly FileService _fileService;
    
    public bool Authenticated { get; private set; }
    
    public ConnectionState(ServerClient client, FileService fileService)
    {
        Client = client;
        _fileService = fileService;
        client.OnReceive<AuthenticationRequest>(req => eventQueue.Enqueue(() => OnAuthenticating(req)));
        client.OnReceive<UserCreateRequest>(req => eventQueue.Enqueue(() => OnCreateUserRequest(req)));
        client.OnMessageReceived<FileRequestMessage>(req => eventQueue.Enqueue(() => OnFileRequested(req)));
    }

    private async Task OnAuthenticating(AuthenticationRequest request)
    {
        if (!await authService.CheckUserAsync(request.Username, request.Password))
        {
            Console.WriteLine(
                $"Authentication error on {Client.RemoteEndpoint}, name: {request.Username}");
            await Client.SendObjectAsync(new AuthenticationReply(false, "Username or password is empty"));
            return;
        }
        
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();
        await Client.SendObjectAsync(new AuthenticationReply(true, null));
        
        User = new User(request.Username, request.Password);
        Authenticated = true;
        Console.WriteLine($"{Client.RemoteEndpoint} authenticated as {request.Username}");
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

    private async Task OnCreateUserRequest(UserCreateRequest request)
    {
        if (request.Username == null || request.Password == null)
        {
            Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a new user: username or password is empty!");
            await Client.SendObjectAsync(new AuthenticationReply(false, "Username or password is empty"));
            return;
        }

        if (authService.Users.ContainsKey(request.Username))
        {
            Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a username that already exists!");
            await Client.SendObjectAsync(new AuthenticationReply(false , "User with that username already exists"));
            return;
        }

        try
        {
            await requestsSemaphore.WaitAsync();
            if (CreateRequests.TryGetValue(request.Username, out _))
            {
                Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a username pending approval!");
                await Client.SendObjectAsync(new AuthenticationReply(false , "User with that username under approval"));
                return;
            }
        
            CreateRequests.TryAdd(request.Username, (this, request));
            Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a new user: {request.Username}");
        }
        finally
        {
            requestsSemaphore.Release();
        }
    }

    public static async Task DenyUser(string username)
    {
        try
        {
            await requestsSemaphore.WaitAsync();
            if (!CreateRequests.Remove(username, out var request))
            {
                Console.WriteLine($"No pending request for \"{username}\"");
                return;
            }
            
            if (request.connection != null)
            {
                request.connection.Client.UnregisterReceive<UserCreateRequest>();
                await request.connection.Client.SendObjectAsync(new AuthenticationReply(false, "Request denied"));
            }
        }
        finally
        {
            requestsSemaphore.Release();
        }
    }

    public static async Task ApproveUser(string username)
    {
        try
        {
            await requestsSemaphore.WaitAsync();
            
            if (!CreateRequests.Remove(username, out var request))
            {
                Console.WriteLine($"No pending request for \"{username}\"");
                return;
            }

            await authService.AddUser(request.request.Username, request.request.Password);
            await authService.SaveUsersAsync();
        
            Console.WriteLine($"Added new user: {request.request.Username}");
        
            if (request.connection != null)
            {
                request.connection.Client.UnregisterReceive<UserCreateRequest>();
                await request.connection.Client.SendObjectAsync(new AuthenticationReply(false, "Request denied"));
            }
        }
        finally
        {
            requestsSemaphore.Release();
        }
    }
}