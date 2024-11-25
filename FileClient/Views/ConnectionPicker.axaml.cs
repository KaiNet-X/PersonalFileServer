using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FileClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace FileClient.Views;

public partial class ConnectionPicker : UserControl
{
    public static readonly DirectProperty<ConnectionPicker, Action> OnConnectProperty =
        AvaloniaProperty.RegisterDirect<ConnectionPicker, Action>(
            nameof(OnConnect),
            c => c.OnConnect,
            (c, val) => c.OnConnect = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string ServerAddress { get; set; } = IPAddress.Loopback.ToString();
    public string Error { get; set; } = string.Empty;

    private Action _onConnect = delegate { };

    public Action OnConnect 
    { 
        get => _onConnect; 
        set => SetAndRaise(OnConnectProperty, ref _onConnect, value); 
    }

    private readonly Client client;

    public ConnectionPicker()
    {
        InitializeComponent();
        DataContext = this;
        client = App.ServiceProvider.GetRequiredService<Client>();
    }
    
    public async void Ok()
    {
        if (!IPAddress.TryParse(ServerAddress, out var address))
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(ServerAddress);
                if (addresses.Any())
                    address = addresses.First();
            }
            catch
            {
                Error = "Unable to resolve server address";
                return;
            }
        }

        if (address is null)
        {
            Error = "Invalid IP address or Hostname.";
            return;
        }
        if (await client.ConnectAsync(ServerAddress, 6969, 0, true))
        {
            Error = string.Empty;
            OnConnect?.Invoke();
        }
        else
            Error = "Could not connect to server";

    }
}