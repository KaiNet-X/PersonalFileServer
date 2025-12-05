using System.Collections.Concurrent;
using System.Collections.Immutable;
using Net.Connection.Clients.Generic;
using Net.Connection.Clients.Tcp;

namespace FileServer;

using System.Collections.Generic;
using System.Text.Json;
using Common;

public class AuthService
{
    private readonly string _passwordPath;
    private Dictionary<string, byte[]> _users;
    private static readonly SemaphoreSlim RequestsSemaphore = new(1, 1);
    public readonly ConcurrentDictionary<string, (ConnectionState Connection, UserCreateRequest Request)> CreateRequests = new ();

    public DictionaryView<string, byte[]> Users => new(_users);

    public AuthService(string workingDirectory)
    {
        _passwordPath = $"{workingDirectory}/Users.json";
    }

    public async Task LoadUsersAsync()
    {
        FileStream? file = null;
        if (!File.Exists(_passwordPath))
        {
            file = File.Create(_passwordPath);
            file.WriteByte((byte)'{');
            file.WriteByte((byte)'}');
            await file.FlushAsync();
            file.Seek(0, SeekOrigin.Begin);
        }

        file ??= File.OpenRead(_passwordPath);

        _users = await JsonSerializer.DeserializeAsync<Dictionary<string, byte[]>>(file);

        await file.DisposeAsync();
    }

    public async Task SaveUsersAsync()
    {
        await using var file = File.Create(_passwordPath);
        await JsonSerializer.SerializeAsync(file, _users);
    }

    public async Task AddUser(string username, byte[] password)
    {
        if (_users.ContainsKey(username)) return;

        var pHash = Crypto.Hash(Crypto.Hash(password));

        _users.Add(username, pHash);
    }

    public async Task RemoveUserAsync(string username)
    {
        if (!_users.ContainsKey(username)) return;

        _users.Remove(username);
        await SaveUsersAsync();
    }

    public async Task<bool> CheckUserAsync(string username, byte[] password)
    {
        if (!_users.TryGetValue(username, out var value)) return false;

        var pHash = Crypto.Hash(Crypto.Hash(password));

        return pHash.SequenceEqual(value);
    }

    public async Task DenyUser(string username)
    {
        try
        {
            await RequestsSemaphore.WaitAsync();
            if (!CreateRequests.Remove(username, out var request))
            {
                Console.WriteLine($"No pending request for \"{username}\"");
                return;
            }

            if (request.Connection != null)
            {
                request.Connection.Client.UnregisterReceive<UserCreateRequest>();
                await request.Connection.Client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Rejected, "Request denied"));
            }
        }
        finally
        {
            RequestsSemaphore.Release();
        }
    }

    public async Task ApproveUser(string username)
    {
        try
        {
            await RequestsSemaphore.WaitAsync();

            if (!CreateRequests.Remove(username, out var request) || request.Request.Username != username)
            {
                Console.WriteLine($"No pending request for \"{username}\"");
                return;
            }

            await AddUser(request.Request.Username, request.Request.Password);
            await SaveUsersAsync();

            Console.WriteLine($"Added new user: {request.Request.Username}");

            if (request.Connection != null)
            {
                await request.Connection.OnUserCreated(request.Request);
            }
        }
        finally
        {
            RequestsSemaphore.Release();
        }
    }

    public async Task OnCreateUserRequest(UserCreateRequest request, ConnectionState connection)
    {
        var client = connection.Client;
        if (request.Username == null || request.Password == null)
        {
            Console.WriteLine($"{client.RemoteEndpoint.Address} requested a new user: username or password is empty!");
            await client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Failure, "Username or password is empty"));
            return;
        }

        if (Users.ContainsKey(request.Username))
        {
            Console.WriteLine($"{client.RemoteEndpoint.Address} requested a username that already exists!");
            await client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.Failure , "User with that username already exists"));
            return;
        }

        try
        {
            await RequestsSemaphore.WaitAsync();
            if (CreateRequests.TryGetValue(request.Username, out _))
            {
                Console.WriteLine($"{client.RemoteEndpoint.Address} requested a username pending approval!");
                await client.SendObjectAsync(new AuthenticationReply(AuthenticationResult.WaitingForApproval , "User with that username under approval"));
                return;
            }

            CreateRequests.TryAdd(request.Username, (connection, request));
            Console.WriteLine($"{client.RemoteEndpoint.Address} requested a new user: {request.Username}");
        }
        finally
        {
            RequestsSemaphore.Release();
        }
    }
}