using System;
using System.Collections.Generic;

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
    // public static readonly DirectProperty<FileTree, ObservableCollection<Node>> NodesProperty =
    //     AvaloniaProperty.RegisterDirect<FileTree, ObservableCollection<Node>>(
    //         nameof(Nodes),
    //         c => c.Nodes,
    //         (c, val) => c.Nodes = val,
    //         defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<FileTree, Node> NodeProperty =
        AvaloniaProperty.RegisterDirect<FileTree, Node>(
            nameof(Node),
            c => c.Node,
            (c, val) => c.Node = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private readonly AuthService authService;
    private readonly FileService fileService;
    private readonly Client client;

    // private ObservableCollection<Node> _nodes;
    // public ObservableCollection<Node> Nodes 
    // { 
    //     get => _nodes;
    //     set => SetAndRaise(NodesProperty, ref _nodes, value);
    // }

    private Node _node;

    public Node Node
    {
        get => _node;
        set => SetAndRaise(NodeProperty, ref _node, value);
    }
    
    public Node? SelectedNode { get; set; }

    public FileTree()
    {
        InitializeComponent();
        authService = App.ServiceProvider.GetRequiredService<AuthService>();
        client = App.ServiceProvider.GetRequiredService<Client>();
        fileService = App.ServiceProvider.GetRequiredService<FileService>();

        DataContext = this;
        //_nodes = new ObservableCollection<Node>();
    }

    public async void Upload(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        //await topLevel.StorageProvider.OpenFolderPickerAsync();
        var picker = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            AllowMultiple = true
        });

        if (picker.Count > 0)
            foreach (var file in picker)
                await fileService.UploadFileAsync(file);
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

public class NodeComparer : IEqualityComparer<Node>
{
    public bool Equals(Node? x, Node? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        return x.Title == y.Title;
    }

    public int GetHashCode(Node obj)
    {
        return obj.Title.GetHashCode();
    }
}