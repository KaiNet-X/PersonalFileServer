using System.Collections.Immutable;

namespace FileServer;

using System.Collections.Generic;
using System.Text.Json;
using Common;

public class AuthService
{
    private readonly string _passwordPath;
    private Dictionary<string, byte[]> _users;

    public static AuthService Instance { get; } = new();
    public DictionaryView<string, byte[]> Users => new(_users);
    
    private AuthService()
    {
        _passwordPath = $"{Directory.GetCurrentDirectory()}/Users.json";
    }
    
    public async Task LoadUsersAsync()
    {
        FileStream file = null;
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
}
