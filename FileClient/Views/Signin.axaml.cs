using Avalonia;
using Avalonia.Controls;
using Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Net.Connection.Clients.Tcp;

namespace FileClient.Views;

public partial class Signin : UserControl
{
    public static readonly DirectProperty<Signin, Func<Task>> SignInCompleteProperty =
        AvaloniaProperty.RegisterDirect<Signin, Func<Task>>(
            nameof(SignInComplete),
            c => c.SignInComplete,
            (c, val) => c.SignInComplete = val,
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

    public string SignInUp { get; set; } = "Sign in";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    private Func<Task>? _signInComplete;

    private readonly Client _client;
    
    public Func<Task> SignInComplete
    {
        get => _signInComplete;
        set => SetAndRaise(SignInCompleteProperty, ref _signInComplete, value);
    }

    private readonly AuthService _authService;

    public Signin(Client client)
    {
        _client = client;
        InitializeComponent();
        DataContext = this;
        _authService = App.ServiceProvider.GetRequiredService<AuthService>();
    }

    public async Task SignIn()
    {
        _authService.SetUser(Username, Password);
        var user = _authService.User.Value;
        
        if (SignInUp == "Sign in")
            await _client.SendObjectAsync(new AuthenticationRequest(Username, user.Password));
        else
            await _client.SendObjectAsync(new UserCreateRequest(Username, user.Password));
    }
}