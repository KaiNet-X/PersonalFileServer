using Net.Connection.Clients.Tcp;

namespace FileClient.ViewModels;

public class ConnectionInfoViewModel : ViewModelBase
{
    public string ServerAddress { get; set; } = string.Empty;
    public ushort ServerPort { get; set; }

    private readonly Client client;

    public ConnectionInfoViewModel(Client client)
    {
        this.client = client;
        ServerAddress = client.RemoteEndpoint.Address.ToString();
        ServerPort = (ushort)client.RemoteEndpoint.Port;
    }
}