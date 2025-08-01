using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Interactivity;

namespace FileClient.Views;

using System.Collections.ObjectModel;
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
                var rootNode = fileTree.GetValue(FileTree.NodeProperty);

                if (rootNode != null)
                    DiffNodes(rootNode, t);
                else
                    fileTree.SetValue(FileTree.NodeProperty, ToNode(t));

                // var nodes = fileTree.GetValue(FileTree.NodesProperty);
                // nodes.Clear();
                // nodes.Add(ToNode(t));

                //fileTree.SetValue(FileTree.NodesProperty, new ObservableCollection<Node>([ToNode(t)]));
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateDimensions();
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
        if (reply.Result)
        {
            await authService.SaveUserAsync();
            await SignInComplete();
            client.UnregisterReceive<AuthenticationReply>();
        }
        else
            ShowSignin();

        UpdateDimensions();
    }

    private void ShowSignin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var signInUp = new SignInOrUp(ReactiveCommand.Create(() => NavSignIn("Sign in")), ReactiveCommand.Create(() => NavSignIn("Sign up")));
            Stack.Children.Clear();
            Stack.Children.Add(signInUp);
        });
    }

    private void NavSignIn(string title)
    {
        Stack.Children.Clear();
        var signin = new Signin(client)
        {
            SignInUp = title
        };
        signin.SetValue(Signin.SignInCompleteProperty, SignInComplete);
        Stack.Children.Add(signin);
        UpdateDimensions();
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
        authService.RemoveUser();
        Stack.Children.Clear();
        var picker = new ConnectionPicker
        {
            OnConnect = Connected
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