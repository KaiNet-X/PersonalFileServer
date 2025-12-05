using Avalonia.Layout;
using Avalonia.Media;

namespace FileClient.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using ReactiveUI;

public partial class MainWindow : Window
{
    private readonly Client client;
    private readonly AuthService authService;

    private FileTree? fileTree;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        var sp = App.ServiceProvider;
        client = sp.GetRequiredService<Client>();
        authService = sp.GetRequiredService<AuthService>();
        var broadcastReceiverService1 = sp.GetRequiredService<BroadcastReceiverService>();

        client.OnReceive<AuthenticationReply>(OnAuthReply);
        client.OnReceive<Tree>(TreeReceived);

        broadcastReceiverService1.StartReceive();
        Closing += (_, _) => broadcastReceiverService1.Dispose();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateDimensions();
    }

    private void TreeReceived(Tree tree)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var rootNode = fileTree.GetValue(FileTree.NodeProperty);

            if (rootNode != null)
                DiffNodes(rootNode, tree);
            else
                fileTree.SetValue(FileTree.NodeProperty, ToNode(tree));
        });
    }
    
    private void UpdateDimensions()
    {
        return;
        var size = new Size();
        Stack.Measure(size);

        var h = size.Height;
        var w = size.Width;
        if (Height < h)
            Height = h;
        if (Width < w)
            Width = w;

        MinHeight = Stack.DesiredSize.Height;
        MinWidth = Stack.DesiredSize.Width;

        //SizeToContent = SizeToContent.Manual;
    }

    public async void Connected()
    {
        try
        {
            Stack.Children.Clear();
            if (!await authService.TryLoadUserAsync())
                ShowSignin();
            else
            {
                var user = authService.User.Value;
                await client.SendObjectAsync(new AuthenticationRequest(user.Username, user.Password));
            }

            UpdateDimensions();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    private async Task OnAuthReply(AuthenticationReply reply)
    {
        switch (reply.Result)
        {
            case AuthenticationResult.Success or AuthenticationResult.Approved:
                await authService.SaveUserAsync();
                await SignInComplete();
                client.UnregisterReceive<AuthenticationReply>();
                break;
            case AuthenticationResult.Failure or AuthenticationResult.Rejected:
                ShowSignin(reply.Reason);
                break;
        }

        UpdateDimensions();
    }

    private void ShowSignin(string? errorText = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var signInUp = new SignInOrUp(
                ReactiveCommand.Create(() => NavSignIn("Sign in", SignIn)),
                ReactiveCommand.Create(() => NavSignIn("Sign up", SignUp)));
            Stack.Children.Clear();
            if (errorText is not null)
                Stack.Children.Add(new TextBlock
                {
                    Text = errorText,
                    Foreground = Brushes.Red
                });
            Stack.Children.Add(signInUp);
        });
    }

    private void NavSignIn(string text, Func<string, string, Task> signInCommand)
    {
        Stack.Children.Clear();
        var signin = new Signin(client, signInCommand, text);
        signin.SetValue(Signin.BackProperty, () => ShowSignin());
        Stack.Children.Add(signin);
        UpdateDimensions();
    }
    
    public async Task SignIn(string username, string password)
    {
        authService.SetUser(username, password);
        var user = authService.User.Value;
        
        await client.SendObjectAsync(new AuthenticationRequest(username, user.Password));
    }
    
    public async Task SignUp(string username, string password)
    {
        authService.SetUser(username, password);
        var user = authService.User.Value;
        
        await client.SendObjectAsync(new UserCreateRequest(username, user.Password));

        Dispatcher.UIThread.Invoke(() =>
        {
            Stack.Children.Clear();
            Stack.Children.Add(new WaitForSignup());
        });
    }
    
    private async Task SignInComplete()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Stack.Children.Clear();
            Stack.Children.Add(new ConnectionInfo(ReactiveCommand.Create(SignOut)));
            Stack.Children.Add(fileTree = new FileTree());
            UpdateDimensions();
        });

        await client.SendMessageAsync(new FileRequestMessage
        {
            RequestType = FileRequestType.Tree
        });
    }

    private async Task SignUp()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Stack.Children.Clear();
            Stack.Children.Add(new WaitForSignup());
        });
    }

    private static Node ToNode(Tree tree)
    {
        if (tree.Nodes.Count == 0)
            return new Node(tree.Value);

        var node = new Node(tree.Value);

        foreach (var subNode in tree.Nodes.Select(ToNode))
        {
            subNode.Parent = node;
            node.SubNodes.Add(subNode);
        }

        return node;
    }

    private void SignOut()
    {
        client.Close();
        
        client.OnReceive<AuthenticationReply>(OnAuthReply);
        client.OnReceive<Tree>(TreeReceived);

        authService.RemoveUser();
        Stack.Children.Clear();
        var picker = new ConnectionPicker
        {
            OnConnect = Connected,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Stack.Children.Add(picker);
    }

    private static void DiffNodes(Node node, Tree tree)
    {
        var treeSet = new HashSet<string>(tree.Nodes.Select(tn => tn.Value));
        var nodeSet = new HashSet<string>(node.SubNodes.Select(sn => sn.Title));

        // Remove deleted nodes

        for (var i = 0; i < node.SubNodes.Count; i++)
        {
            var subNode = node.SubNodes[i];
            if (treeSet.Contains(subNode.Title)) continue;
            node.SubNodes.Remove(subNode);
            i--;
        }

        foreach (var tr in tree.Nodes)
        {
            if (nodeSet.Contains(tr.Value))
                DiffNodes(node.SubNodes.First(sn => sn.Title == tr.Value), tr);
            else
            {
                var subNode = ToNode(tr);
                subNode.Parent = node;
                node.SubNodes.Add(subNode);
            }
        }
    }
}