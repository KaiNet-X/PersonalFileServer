using System;
using System.Linq;
using System.Net;
using Net.Connection.Clients.Tcp;

namespace FileClient.ViewModels;

public class ConnectionPickerViewModel : ViewModelBase
{
    private string serverAddress = IPAddress.Loopback.ToString();
    private string error = string.Empty;
    private Action onConnect = delegate { };

    private readonly Client client;
    
    public string ServerAddress
    {
        get => serverAddress;
        set => SetField(ref serverAddress, value);
    }

    public string Error
    {
        get => error;
        set => SetField(ref error, value);
    }
    
    public Action OnConnect 
    { 
        get => onConnect; 
        set => SetField(ref onConnect, value); 
    }

    public ConnectionPickerViewModel(Client client)
    {
        this.client = client;
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