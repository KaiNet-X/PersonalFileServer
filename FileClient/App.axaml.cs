using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileClient.ViewModels;
using FileClient.Views;
using KaiNet.Net;

namespace FileClient;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }
        var cl = new Client();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IAppService,
        base.OnFrameworkInitializationCompleted();
    }
}