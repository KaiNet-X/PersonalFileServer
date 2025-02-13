using Net;

namespace FileClient;

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using Views;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; }
    
    static App()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<Client>();
        serviceCollection.AddSingleton(AuthService.Instance);
        serviceCollection.AddSingleton<FileService>();
        serviceCollection.AddSingleton(BroadcastReceiverService.Instance);
        
        var services = serviceCollection.BuildServiceProvider();
        services.GetRequiredService<Client>().OnDisconnected(inf =>
        {
            Environment.Exit(0);
        });
        
        ServiceProvider = services;
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (_, _) =>
            {
                var client = ServiceProvider.GetService<Client>();
                if (client is {ConnectionState: ConnectionState.CONNECTED})
                    client.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}