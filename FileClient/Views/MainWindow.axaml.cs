using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using Common;
using FileClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Net.Connection.Clients.Tcp;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ReactiveUI;

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

        client.OnReceive<AuthenticationReply>(OnAuthReply);
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
        if (!await authService.TryLoadUserAsync())
            ShowSignin();
        else
            await client.SendObjectAsync(new AuthenticationRequest(authService.User.Username, authService.User.Password));
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
            RequestType = FileRequestType.Tree,
            User = authService.User
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
        var picker = new ConnectionPicker();
        picker.OnConnect = Connected;
        Stack.Children.Add(picker);
    }
}