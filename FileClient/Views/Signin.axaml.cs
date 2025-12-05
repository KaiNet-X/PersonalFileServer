using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using ReactiveUI;

namespace FileClient.Views;

public partial class Signin : UserControl
{
    public static readonly DirectProperty<Signin, Action> BackProperty =
        AvaloniaProperty.RegisterDirect<Signin, Action>(
            nameof(BackNavigate),
            c => c.BackNavigate,
            (c, val) => c.BackNavigate = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    
    public static readonly DirectProperty<Signin, string> UsernameProperty =
        AvaloniaProperty.RegisterDirect<Signin, string>(
            nameof(Username),
            c => c.Username,
            (c, val) => c.Username = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<Signin, string> PasswordProperty =
        AvaloniaProperty.RegisterDirect<Signin, string>(
            nameof(Password),
            c => c.Password,
            (c, val) => c.Password = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string SignInUp { get; private set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public Func<string, string, Task>  SignInCommand { get; private set; }
    
    private readonly Client _client;

    [field: AllowNull]
    public Action BackNavigate
    {
        get;
        set => SetAndRaise(BackProperty, ref field, value);
    }

    private readonly AuthService _authService;

    public Signin(Client client, Func<string, string, Task> signInCommand, string signInText)
    {
        SignInUp = signInText;
        _client = client;
        SignInCommand = signInCommand;
        
        InitializeComponent();
        
        DataContext = this;
        _authService = App.ServiceProvider.GetRequiredService<AuthService>();
    }

    public async Task SignIn()
    {
        await SignInCommand(Username, Password);
        _authService.SetUser(Username, Password);
        var user = _authService.User.Value;
        
        if (SignInUp == "Sign in")
            await _client.SendObjectAsync(new AuthenticationRequest(Username, user.Password));
        else
            await _client.SendObjectAsync(new UserCreateRequest(Username, user.Password));
    }
    
    private async void KeyUp(object? sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key != Key.Return) return;
            Focus();
            await SignIn();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BackNavigate();
    }
}