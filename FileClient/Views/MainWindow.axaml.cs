using Avalonia.Controls;
using Common;
using FileClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System.Threading.Tasks;

namespace FileClient.Views;

public partial class MainWindow : Window
{

    private readonly Client client;
    private readonly AuthService authService;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        client = Extensions1.ServiceProvider.GetService<Client>();
        authService = Extensions1.ServiceProvider.GetService<AuthService>();
    }

    public async Task Connected()
    {
        Stack.Children.Clear();
        Stack.Children.Add(new ConnectionInfo());
        if (!await authService.TryLoadUserAsync())
            Stack.Children.Add(new Signin());
        else
            await client.SendMessageAsync(new FileRequestMessage()
            {
                RequestType = FileRequestType.Tree,
                User = authService.User,
            });
    }
}