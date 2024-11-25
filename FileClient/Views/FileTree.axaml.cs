namespace FileClient.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

public partial class FileTree : UserControl
{
    public static readonly DirectProperty<FileTree, ObservableCollection<Node>> NodesProperty =
        AvaloniaProperty.RegisterDirect<FileTree, ObservableCollection<Node>>(
            nameof(Nodes),
            c => c.Nodes,
            (c, val) => c.Nodes = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private readonly AuthService authService;
    private readonly FileService fileService;
    private readonly Client client;

    private ObservableCollection<Node> _nodes;
    public ObservableCollection<Node> Nodes 
    { 
        get => _nodes;
        set => SetAndRaise(NodesProperty, ref _nodes, value);
    }

    public Node? SelectedNode { get; set; }

    public FileTree()
    {
        InitializeComponent();
        authService = App.ServiceProvider.GetRequiredService<AuthService>();
        client = App.ServiceProvider.GetRequiredService<Client>();
        fileService = App.ServiceProvider.GetRequiredService<FileService>();

        DataContext = this;
        _nodes = new ObservableCollection<Node>();
    }

    public async void Upload(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var picker = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            AllowMultiple = false
        });

        if (picker.Count > 0)
            await fileService.UploadFileAsync(picker[0].Path.AbsolutePath, picker[0].Name);
    }

    public async void Download(object sender, RoutedEventArgs e)
    {
        if (SelectedNode == null)
            return;
        
        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Download,
            PathRequest = SelectedNode.Title
        });
    }

    public async void Delete(object sender, RoutedEventArgs e)
    {
        if (SelectedNode == null)
            return;
        
        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Delete,
            PathRequest = SelectedNode.Title
        });
    }
}

public class Node
{
    public ObservableCollection<Node>? SubNodes { get; set; }
    public string Title { get; }

    public Node(string title)
    {
        Title = title;
    }

    public Node(string title, ObservableCollection<Node> subNodes)
    {
        Title = title;
        SubNodes = subNodes;
    }
}