using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FileClient.Extensions;
using FileClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;

namespace FileClient.Views;

public partial class Signin : UserControl
{
    public Signin()
    {
        InitializeComponent();
        DataContext = new SigninViewModel();
    }
}