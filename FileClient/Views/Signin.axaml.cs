using Avalonia;
using Avalonia.Controls;
using Common;
using FileClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FileClient.Views;

public partial class Signin : UserControl
{
    public static readonly DirectProperty<Signin, Func<Task>> SignInCompleteProperty =
        AvaloniaProperty.RegisterDirect<Signin, Func<Task>>(
            nameof(SignInComplete),
            c => c.SignInComplete,
            (c, val) => c.SignInComplete = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string Username { get; set; }
    public string Password { get; set; }

    private Func<Task> _signInComplete;

    public Func<Task> SignInComplete
    {
        get => _signInComplete;
        set => SetAndRaise(SignInCompleteProperty, ref _signInComplete, value);
    }

    private readonly AuthService _authService;

    public Signin()
    {
        InitializeComponent();
        DataContext = this;
        _authService = Extensions1.ServiceProvider.GetService<AuthService>();
    }

    public async Task SignIn()
    {
        await _authService.SetUser(new User(Username, Password));
        if (SignInComplete != null) 
            await SignInComplete();
    }
}