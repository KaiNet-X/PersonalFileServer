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
using Avalonia.Threading;

namespace FileClient.Views;

public partial class ConnectionPicker : UserControl
{
    private ConnectionPickerViewModel vm;
    private readonly Client client;
    
    public Action OnConnect
    {
        get => vm.OnConnect;
        set => vm.OnConnect = value;
    }
    
    public ConnectionPicker()
    {
        InitializeComponent();
        App.ServiceProvider.GetRequiredService<BroadcastReceiverService>().OnServer = OnServerFound;
        client = App.ServiceProvider.GetRequiredService<Client>();
        DataContext = vm = new ConnectionPickerViewModel(client);
    }
    
    private void OnServerFound(string address)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ServerAddresses.Items.Add(address);
        });
    }

    private void ServerSelected(object sender, SelectionChangedEventArgs e)
    {
        vm.ServerAddress = ServerAddresses.SelectedItem as string;
    }
}