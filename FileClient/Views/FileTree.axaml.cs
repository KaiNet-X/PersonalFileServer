using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;

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
    public static readonly DirectProperty<FileTree, Node> NodeProperty =
        AvaloniaProperty.RegisterDirect<FileTree, Node>(
            nameof(Node),
            c => c.Node,
            (c, val) => c.Node = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<FileTree, Node> SelectedNodeProperty =
        AvaloniaProperty.RegisterDirect<FileTree, Node>(
            nameof(SelectedNode),
            c => c.SelectedNode,
            (c, val) => c.SelectedNode = val,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<FileTree, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<FileTree, bool>(
            nameof(IsSelected),
            c => c.IsSelected);
    
    private readonly AuthService authService;
    private readonly FileService fileService;
    private readonly Client client;

    private Node _node;
    private Node? _selectedNode;
    private bool _isSelected;
    
    public Node Node
    {
        get => _node;
        set => SetAndRaise(NodeProperty, ref _node, value);
    }

    public Node? SelectedNode
    {
        get => _selectedNode;
        set
        {
            SetAndRaise(SelectedNodeProperty, ref _selectedNode, value);
            IsSelected = SelectedNode is not null;
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
    }
    
    public FileTree()
    {
        InitializeComponent();
        authService = App.ServiceProvider.GetRequiredService<AuthService>();
        client = App.ServiceProvider.GetRequiredService<Client>();
        fileService = App.ServiceProvider.GetRequiredService<FileService>();

        DataContext = this;
        AddHandler(DragDrop.DropEvent, OnDrop);
        //_nodes = new ObservableCollection<Node>();
    }

    public async void UploadFile(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var picker = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true
        });

        if (picker.Count > 0)
            foreach (var file in picker)
                await fileService.UploadFileAsync(file);
    }

    public async void UploadFolder(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var picker = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = true
        });

        if (picker.Count > 0)
            foreach (var folder in picker)
                await fileService.UploadFolderAsync(folder);

    }
    
    public async void Download(object sender, RoutedEventArgs e)
    {
        if (SelectedNode == null)
            return;
        
        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Download,
            PathRequest = GetPath(SelectedNode)
        });
    }

    public async void Delete(object sender, RoutedEventArgs e)
    {
        if (SelectedNode == null)
            return;

        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Delete,
            PathRequest = GetPath(SelectedNode)
        });
    }

    private string GetPath(Node node)
    {
        var path = node.Title;
        
        node = SelectedNode.Parent;
        
        while (node != null)
        {
            path = $"{node.Title}{Path.DirectorySeparatorChar}{path}";

            node = node.Parent;
        }
        
        return path;
    }
    
    private async Task OnDrop(object sender, DragEventArgs e)
    {
        var names = e.Data.GetFiles();
        
        if (names is not null)
            foreach (var item in names)
            {
                if (item is IStorageFile file)
                    await fileService.UploadFileAsync(file);
                else if (item is IStorageFolder folder)
                    await fileService.UploadFolderAsync(folder);
            }
    }
}

public class Node
{
    public ObservableCollection<Node> SubNodes { get; }
    
    public Node? Parent { get; set; }
    
    public string Title { get; }

    public Node(string title)
    {
        Title = title;
        SubNodes = new ObservableCollection<Node>();
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