using System.Collections.Generic;
using System.Linq;

namespace FileClient.Views;

using Avalonia.Controls;
using Avalonia.Threading;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ReactiveUI;

public partial class MainWindow : Window
{
    private readonly Client client;
    private readonly AuthService authService;
    private readonly BroadcastReceiverService broadcastReceiverService;
    
    private FileTree? fileTree = null;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        var sp = App.ServiceProvider;
        client = sp.GetRequiredService<Client>();
        authService = sp.GetRequiredService<AuthService>();
        broadcastReceiverService = sp.GetRequiredService<BroadcastReceiverService>();
        
        client.OnReceive<AuthenticationReply>(OnAuthReply);
        client.OnReceive<Tree>(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                fileTree.SetValue(FileTree.NodesProperty, new ObservableCollection<Node>([ToNode(t)]));
                // var nodes = fileTree.GetValue(FileTree.NodesProperty);
                // if (nodes is null)
                // else if (nodes.Count == 0)
                //     nodes.Add(ToNode(t));
                // else
                //     DiffNodes(nodes[0], ToNode(t));
            });
        });
        
        broadcastReceiverService.StartReceive();
        Closing += (_, __) => broadcastReceiverService.Dispose();
    }

    public async void Connected()
    {
        Stack.Children.Clear();
        if (!await authService.TryLoadUserAsync())
            ShowSignin();
        else
        {
            var user = authService.User.Value;
            await client.SendObjectAsync(new AuthenticationRequest(user.Username, user.Password));
        }
    }

    private async Task OnAuthReply(AuthenticationReply reply)
    {
        if (reply.Result)
        {
            await authService.SaveUserAsync();
            await SignInComplete();
            client.UnregisterReceive<AuthenticationReply>();
        }
        else
            ShowSignin();
    }

    void ShowSignin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var signInUp = new SignInOrUp(ReactiveCommand.Create(() => NavSignIn("Sign in")), ReactiveCommand.Create(() => NavSignIn("Sign up")));
            Stack.Children.Clear();
            Stack.Children.Add(signInUp);
        });
    }
    
    public void NavSignIn(string title)
    {
        Stack.Children.Clear();
        var signin = new Signin(client);
        signin.SignInUp = title;
        signin.SetValue(Signin.SignInCompleteProperty, SignInComplete);
        Stack.Children.Add(signin);
    }
    
    public async Task SignInComplete()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Stack.Children.Clear();
            Stack.Children.Add(new ConnectionInfo(ReactiveCommand.Create(SignOut)));
            Stack.Children.Add(fileTree = new FileTree());
        });

        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Tree
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

    private void SignOut()
    {
        client.Close();
        client.OnReceive<AuthenticationReply>(OnAuthReply);
        authService.RemoveUser();
        Stack.Children.Clear();
        var picker = new ConnectionPicker
        {
            OnConnect = Connected
        };
        Stack.Children.Add(picker);
    }

    private void DiffNodes(Node oldNode, Node newNode)
    {
        var toRemove = new List<Node>();
        
        var comparer = new NodeComparer();
        
        var oldNodes = new HashSet<Node>(oldNode.SubNodes ?? [], comparer);
        var newNodes = new HashSet<Node>(newNode.SubNodes ?? [], comparer);

        foreach (var node in oldNodes)
        {
            if (!newNodes.Contains(node))
                oldNodes.Remove(node);
            
            DiffNodes(node, newNodes.First(n => n.Title == node.Title));
        }

        foreach (var node in newNodes)
        {
            if (!newNodes.Contains(node))
                newNodes.Add(node);
        }
    }

    private void DiffNodes(Node node, Tree tree)
    {
        var treeSet = new HashSet<Tree>(tree);
        var nodeSet = new HashSet<Node>(node.SubNodes);
        
        //var treenodes = new HashSet(tree.Nodes)
        foreach (var n in tree.Nodes)
        {
            
        }
    }
}