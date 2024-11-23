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
        client.OnReceive<AuthenticationObj>(OnAuthenticating);
        client.OnReceive<UserCreateRequest>(OnCreateUserRequest);
        client.OnMessageReceived<FileRequestMessage>(OnFileRequested);
    }

    private async Task OnAuthenticating(AuthenticationObj obj)
    {
        if (!await _authService.CheckUserAsync(obj.Username, obj.Password))
        {
            Console.WriteLine($"Authentication error on {Client.RemoteEndpoint.Address}, name: {obj.Username}");
            return;
        }
        Authenticated = true;
    }

    private async Task OnFileRequested(FileRequestMessage msg)
    {
        if (!Authenticated)
        {
            Console.WriteLine($"Unauthenticated client {Client.RemoteEndpoint.Address} sent a file request!");
            return;
        }
        
        await _fileService.HandleFileRequest(msg, Client);
    }

    private async Task OnCreateUserRequest(UserCreateRequest msg)
    {
        if (msg.Username == null || msg.Password == null)
        {
            Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a new user: username or password is empty!");
            Client.SendObject(new AuthenticationFailed("Username or password is empty"));
            return;
        }

        if (_authService.Users.ContainsKey(msg.Username))
        {
            Console.WriteLine($"{Client.RemoteEndpoint.Address} requested a username that already exists!");
            Client.SendObject(new AuthenticationFailed("User with that username already exists"));
            return;
        }
        
        while (true)
        {
            Console.Write($"{Client.RemoteEndpoint.Address} requested a new user: {msg.Username}. Create user? [y,n] ");
            var answer = Console.ReadLine();
            if (answer.ToLower() == "y")
                break;
            if (answer.ToLower() == "n")
                return;
        }
        
        await _authService.AddUser(msg.Username, msg.Password);
        Client.SendObject(new UserCreated());

        Authenticated = true;
    }
}

public sealed record AuthenticationObj(string Username, string Password);
public sealed record UserCreateRequest(string Username, string Password);
public sealed record AuthenticationFailed(string Reason);
public sealed record UserCreated;