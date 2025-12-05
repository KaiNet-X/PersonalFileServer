using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FileClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;

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

        var addr = BroadcastReceiverService.Instance.Addresses;
        foreach (var address in addr)
            ServerAddresses.Items.Add(address);

        if (addr.Length > 0)
        {
            TextBox.Text = addr[0];
            //ServerAddresses.SelectedIndex = 0;
        }
        
        var broadcastReceiver = App.ServiceProvider.GetRequiredService<BroadcastReceiverService>();
        broadcastReceiver.OnServer = OnServerFound;

        foreach (var address in broadcastReceiver.ServerAddresses)
            ServerAddresses.Items.Add(address);

        client = App.ServiceProvider.GetRequiredService<Client>();
        DataContext = vm = new ConnectionPickerViewModel(client);
    }

    private void OnServerFound(string address)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ServerAddresses.Items.Add(address);
            if (ServerAddresses.SelectedIndex < 0)
                ServerAddresses.SelectedIndex = 0;
        });
    }

    private void ServerSelected(object sender, SelectionChangedEventArgs e)
    {
        vm.ServerAddress = ServerAddresses.SelectedItem as string;
    }
}