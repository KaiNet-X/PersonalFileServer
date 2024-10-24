namespace FileClient.ViewModels;

using System.Net;

public class ConnectionPickerViewModel : ViewModelBase
{
    public string ServerAddress { get; set; } = IPAddress.Loopback.ToString();

    public void Ok()
    {
        if (IPAddress.TryParse(ServerAddress, out var address))
        {
            
        }
    }
}