using Common;
using Net.Connection.Clients.Tcp;

namespace FileServer;

public class ConnectionState
{
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
            ConsoleManager.QueueLine($"Authentication error on {Client.RemoteEndpoint.Address}, name: {request.Username}");
            return;
        }
        
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();

        Authenticated = true;
    }

    private async Task OnFileRequested(FileRequestMessage msg)
    {
        if (!Authenticated)
        {
            ConsoleManager.QueueLine($"Unauthenticated client {Client.RemoteEndpoint.Address} sent a file request!");
            return;
        }
        
        await _fileService.HandleFileRequest(msg, Client);
    }

    private async Task OnCreateUserRequest(UserCreateRequest msg)
    {
        if (msg.Username == null || msg.Password == null)
        {
            ConsoleManager.QueueLine($"{Client.RemoteEndpoint.Address} requested a new user: username or password is empty!");
            Client.SendObject(new AuthenticationReply(false, "Username or password is empty"));
            return;
        }

        if (_authService.Users.ContainsKey(msg.Username))
        {
            ConsoleManager.QueueLine($"{Client.RemoteEndpoint.Address} requested a username that already exists!");
            Client.SendObject(new AuthenticationReply(false , "User with that username already exists"));
            return;
        }
        
        while (true)
        {
            var answer = await ConsoleManager.Prompt($"{Client.RemoteEndpoint.Address} requested a new user: {msg.Username}. Create user? [y,n] ");
            if (answer.ToLower() == "y")
                break;
            if (answer.ToLower() == "n")
            {
                Client.UnregisterReceive<UserCreateRequest>();
                await Client.SendObjectAsync(new AuthenticationReply(false, "Request denied"));
                return;
            }
        }
        
        await _authService.AddUser(msg.Username, msg.Password);
        
        Client.UnregisterReceive<AuthenticationRequest>();
        Client.UnregisterReceive<UserCreateRequest>();

        await Client.SendObjectAsync(new AuthenticationReply(true, "User approved"));

        Authenticated = true;
    }
}