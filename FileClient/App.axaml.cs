namespace FileClient;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileClient.ViewModels;
using FileClient.Views;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; }
    
    static App()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<Client>();
        serviceCollection.AddSingleton(AuthService.Instance);
        serviceCollection.AddSingleton<FileService>();

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
        }

        base.OnFrameworkInitializationCompleted();
    }
}