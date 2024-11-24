using System.Windows.Input;
using Avalonia.Controls;
using FileClient.Extensions;
using FileClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;

namespace FileClient.Views;

public partial class ConnectionInfo : UserControl
{
    
    public ConnectionInfo(ICommand onSignOutCommand)
    {
        InitializeComponent();
        DataContext = new ConnectionInfoViewModel(Extensions1.ServiceProvider.GetService<Client>(), onSignOutCommand);
    }
}