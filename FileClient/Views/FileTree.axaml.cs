using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace FileClient.Views;

using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;

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

    private readonly FileService fileService;
    private readonly Client client;
    private IStorageProvider _storageProvider => TopLevel.GetTopLevel(this).StorageProvider;

    private Node _node;
    private Node? _selectedNode;
    private bool _isSelected;
    
    private bool CanOpenFilePicker { get; } = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
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
        App.ServiceProvider.GetRequiredService<AuthService>();
        client = App.ServiceProvider.GetRequiredService<Client>();
        fileService = App.ServiceProvider.GetRequiredService<FileService>();

        DataContext = this;
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    public async void UploadFile(object sender, RoutedEventArgs e)
    {

        var picker = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true
        });

        if (picker.Count > 0)
            foreach (var file in picker)
                await fileService.UploadFileAsync(file);
    }

    public async void UploadFolder(object sender, RoutedEventArgs e)
    {
        var picker = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
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

        if (SelectedNode.SubNodes.Any())
            await fileService.DownloadFolderAsync(SelectedNode, GetPath(SelectedNode));
        else
            await fileService.DownloadFileAsync(GetPath(SelectedNode));
    }

    public async void Delete(object sender, RoutedEventArgs e)
    {
        if (SelectedNode == null)
            return;

        await client.SendObjectAsync(new FileRequest(FileRequestType.Delete, Guid.NewGuid(), GetPath(SelectedNode)));
    }

    public void LaunchFileManager(object sender, RoutedEventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var fileOpener = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = FileService.DownloadDirectory
                }
            };
            fileOpener.Start();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var fileOpener = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = FileService.DownloadDirectory
                }
            };
            fileOpener.Start();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var dbusShowItemsProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dbus-send",
                    Arguments = $"--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"file://{FileService.DownloadDirectory}\" string:\"\"",
                    UseShellExecute = true
                }
            };
            dbusShowItemsProcess.Start();
        }
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