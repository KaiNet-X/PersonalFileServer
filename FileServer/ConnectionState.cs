using System.Net;
using Common;
using Net.Connection.Clients.Tcp;

namespace FileServer;

public class ConnectionState
{
    public User User { get; private set; }
    public IPEndPoint Endpoint  => Client.RemoteEndpoint;
    
    public readonly ServerClient Client;
    private readonly AuthService _authService;
    private readonly FileService _fileService;
    
    public bool Authenticated { get; private set; }
    
    public ConnectionState(ServerClient client, AuthService authService, FileService fileService)
    {
        Client = client;
        _authService = authService;
        _fileService = fileService;
        client.OnReceive<AuthenticationRequest>(OnAuthenticating);
        client.OnReceive<UserCreateRequest>(OnCreateUserRequest);
        client.OnMessageReceived<FileRequestMessage>(OnFileRequested);
    }

    private async Task OnAuthenticating(AuthenticationRequest request)
    {
        if (!await _authService.CheckUserAsync(request.Username, request.Password))
        {
            ConsoleManager.QueueLine(
                $"Authentication error on {Client.RemoteEndpoint.Address}, name: {request.Username}");
            await Client.SendObjectAsync(new AuthenticationReply(false, "Username or password is empty"));
            return;
        }
        
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();
        await Client.SendObjectAsync(new AuthenticationReply(true, null));
        
        User = new User(request.Username, request.Password);
        Authenticated = true;
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
            ConsoleManager.QueueLine($"{Client.RemoteEndpoint.Address} requested a new user: username or password is empty!");
            await Client.SendObjectAsync(new AuthenticationReply(false, "Username or password is empty"));
            return;
        }

        if (_authService.Users.ContainsKey(request.Username))
        {
            ConsoleManager.QueueLine($"{Client.RemoteEndpoint.Address} requested a username that already exists!");
            await Client.SendObjectAsync(new AuthenticationReply(false , "User with that username already exists"));
            return;
        }
        
        while (true)
        {
            var answer = await ConsoleManager.Prompt($"{Client.RemoteEndpoint.Address} requested a new user: {request.Username}. Create user? [y,n] ");
            if (answer.ToLower() == "y")
                break;
            if (answer.ToLower() == "n")
            {
                Client.UnregisterReceive<UserCreateRequest>();
                await Client.SendObjectAsync(new AuthenticationReply(false, "Request denied"));
                return;
            }
        }
        
        await _authService.AddUser(request.Username, request.Password);
        await _authService.SaveUsersAsync();
        
        ConsoleManager.QueueLine($"Added new user: {request.Username}");
        
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();
        
        await Client.SendObjectAsync(new AuthenticationReply(true, "User approved"));
        User = new User(request.Username, request.Password);
        Authenticated = true;
    }
}