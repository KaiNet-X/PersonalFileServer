using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FileClient.Extensions;
using FileClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System;
using System.Net;
using System.Windows.Input;

namespace FileClient.Views;

public partial class ConnectionPicker : UserControl
{
    private static readonly DirectProperty<ConnectionPicker, Action> onConnectProperty =
        AvaloniaProperty.RegisterDirect<ConnectionPicker, Action>(
            nameof(OnConnect),
            c => c.OnConnect,
            (c, val) => c.OnConnect = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string ServerAddress { get; set; } = IPAddress.Loopback.ToString();
    public string Error { get; set; } = string.Empty;

    private Action _onConnect;

    public Action OnConnect 
    { 
        get => _onConnect; 
        set => SetAndRaise(OnConnectProperty, ref _onConnect, value); 
    }

    public static DirectProperty<ConnectionPicker, Action> OnConnectProperty => onConnectProperty;

    private readonly Client client;

    public ConnectionPicker()
    {
        InitializeComponent();
        DataContext = this;
        client = Extensions1.ServiceProvider.GetService<Client>();
    }
    
    public async void Ok()
    {
        if (IPAddress.TryParse(ServerAddress, out var address))
        {
            if (await client.ConnectAsync(new IPEndPoint(address, 6969)))
            {
                Error = string.Empty;
                OnConnect?.Invoke();
            }
            else
                Error = "Could not connect to server";
        }
    }
}