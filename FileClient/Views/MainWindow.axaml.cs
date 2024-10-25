using Avalonia.Controls;
using Avalonia.Threading;
using Common;
using FileClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FileClient.Views;

public partial class MainWindow : Window
{

    private readonly Client client;
    private readonly AuthService authService;
    private FileTree fileTree = null;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        client = Extensions1.ServiceProvider.GetService<Client>();
        authService = Extensions1.ServiceProvider.GetService<AuthService>();

        client.OnReceive<Tree>(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                fileTree.SetValue(FileTree.NodesProperty, new ObservableCollection<Node>([ToNode(t)]));
            });
        });
    }

    public async void Connected()
    {
        Stack.Children.Clear();
        Stack.Children.Add(new ConnectionInfo());
        if (!await authService.TryLoadUserAsync())
        {
            var signin = new Signin();
            signin.SetValue(Signin.SignInCompleteProperty, SignInComplete);
            Stack.Children.Add(signin);
        }
        else
        {
            Stack.Children.Add(fileTree = new FileTree());
            await client.SendMessageAsync(new FileRequestMessage()
                {
                    RequestType = FileRequestType.Tree,
                    User = authService.User,
                });
        }
    }

    public async Task SignInComplete()
    {
        Stack.Children.Clear();
        Stack.Children.Add(new ConnectionInfo());
        Stack.Children.Add(fileTree = new FileTree());

        await client.SendMessageAsync(new FileRequestMessage()
        {
            RequestType = FileRequestType.Tree,
            User = authService.User,
        });
    }

    private Node ToNode(Tree tree)
    {
        if (tree.Nodes.Count == 0)
            return new Node(tree.Value);

        var nodes = new ObservableCollection<Node>();

        foreach (var node in tree)
            nodes.Add(ToNode(node));

        return new Node(tree.Value) { SubNodes = nodes };
    }
}