using System.Windows.Input;
using Net.Connection.Clients.Tcp;

namespace FileClient.ViewModels;

public class ConnectionInfoViewModel : ViewModelBase
{
    public string ServerAddress { get; set; } = string.Empty;
    public ushort ServerPort { get; set; }

    private readonly Client client;
    
    public ICommand OnSignOutCommand { get; set; }

    public ConnectionInfoViewModel(Client client, ICommand onSignOutCommand)
    {
        this.client = client;
        ServerAddress = client.RemoteEndpoint.Address.ToString();
        ServerPort = (ushort)client.RemoteEndpoint.Port;
        OnSignOutCommand = onSignOutCommand;
    }
}