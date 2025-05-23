namespace WinformsFileClient;

using System.Net;
using Microsoft.VisualBasic;
using System.Diagnostics;
using Net.Connection.Clients.Tcp;
using System.Linq;
using Common;

public partial class MainForm : Form
{
    private Client _client
    {
        get => Program.Client;
        set => Program.Client = value;
    }

    List<Guid> inProgress = new List<Guid>();

    private string _path;

    string _dir = @$"{Directory.GetCurrentDirectory()}\Files";
    SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    FileStream current = null;

    public MainForm()
    {
        InitializeComponent();

        Directory.CreateDirectory(_dir);

        _client = new Client();

        _client.OnAnyMessage(mb =>
        {
            MessageBox.Show(mb.MessageType);
        });
        _client.OnReceive<Tree>(t =>
        {
            Invoke(() =>
            {
                UpdateNodes(t, treeView.Nodes);
                //treeView.Nodes.Add(ToNode(t));
            });
        });
        _client.OnMessageReceived<FileRequestMessage>((Action<FileRequestMessage>)(async msg =>
        {
            Directory.CreateDirectory(_dir);

            if (!inProgress.Contains(msg.RequestId))
            {
                inProgress.Add(msg.RequestId);
                current = File.Create($@"{_dir}\{msg.FileName}");
            }

            if (msg.EndOfMessage)
                inProgress.Remove(msg.RequestId);

            if (!msg.EndOfMessage && !inProgress.Contains(msg.RequestId))
            {
                inProgress.Add(msg.RequestId);
                current = File.Create($@"{_dir}\{msg.FileName}");
                await current.WriteAsync(msg.FileData.AsMemory());
            }
            else
            {
                await _semaphore.WaitAsync();
                try
                {
                    await current.WriteAsync(msg.FileData.AsMemory());
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            if (msg.EndOfMessage)
                current.Dispose();
        }));

        _client.OnAnyChannel(async obj =>
        {
            var bytes = await obj.ReceiveBytesAsync();

            var dir = @$"{Directory.GetCurrentDirectory()}\Files";
            Directory.CreateDirectory(dir);

            using (FileStream fs = File.Create($@"{dir}\{"FFFF"}"))
            {
                await fs.WriteAsync(bytes);
            }
        });

        _client.OnDisconnected(obj =>
        {
            //MessageBox.Show("Server disconnected");

            UpdateUi();

            _ = Connect();
        });
    }

    private async void downloadButton_Click(object sender, EventArgs e)
    {
        await _client.SendMessageAsync(new FileRequestMessage { RequestType = FileRequestType.Download, PathRequest = _path, User = Auth.User });
    }

    private async void deleteFileButton_Click(object sender, EventArgs e)
    {
        await _client.SendMessageAsync(new FileRequestMessage { RequestType = FileRequestType.Delete, PathRequest = _path, User = Auth.User });
    }

    private async void uploadButton_Click(object sender, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            using FileStream fs = File.OpenRead(ofd.FileName);
            var newMsg = new FileRequestMessage() { RequestType = FileRequestType.Upload, PathRequest = ofd.SafeFileName, User = Auth.User };
            newMsg.FileData = new byte[fs.Length];
            await fs.ReadAsync(newMsg.FileData);
            await _client.SendMessageAsync(newMsg);
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _client.Close();
        Task.Delay(1000).Wait();
    }

    private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        _path = e.Node.FullPath;
    }

    private TreeNode ToNode(Tree tree)
    {
        TreeNode treeNode = new TreeNode();
        treeNode.Text = tree.Value;

        foreach (var node in tree.Nodes)
            treeNode.Nodes.Add(ToNode(node));
        return treeNode;
    }

    private void UpdateNodes(Tree tree, TreeNodeCollection treeNodes)
    {
        for (int i = 0; i < treeNodes.Count; i++)
        {
            if (!tree.Nodes.Any(t => t.Value == treeNodes[i].Text))
            {
                treeNodes.RemoveAt(i);
                i--;
            }
        }

        foreach (TreeNode t in treeNodes)
            UpdateNodes(tree.First(x => x.Value == t.Text), t.Nodes);

        foreach (Tree t in tree)
        {
            bool match = false;
            foreach (TreeNode node in treeNodes)
                if (t.Value == node.Text)
                    match = true;

            if (!match)
                treeNodes.Add(ToNode(t));
        }
    }

    private void directoryButton_Click(object sender, EventArgs e) =>
        Process.Start("explorer.exe", _dir);

    private async Task Connect()
    {
        try
        {
            var addr = await Task.Run(() => IPAddress.Parse(Interaction.InputBox("What is a valid server address?", "Address", "127.0.0.1")));

            await _client.ConnectAsync(addr, 6969, 15, true);
            await _client.SendMessageAsync(new FileRequestMessage { RequestType = FileRequestType.Tree, User = Auth.User });

            UpdateUi();

        }
        catch (Exception ex)
        {

        }
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        await Connect();
    }

    void UpdateUi()
    {
        if (_client == null || _client.ConnectionState != Net.ConnectionState.CONNECTED)
        {
            cAddr.Text = cPort.Text = sAddr.Text = sPort.Text = "NONE";
            connectedLabel.Text = "false";
            downloadButton.Enabled = uploadButton.Enabled = deleteFileButton.Enabled = false;
        }

        cAddr.Text = _client?.LocalEndpoint.Address.ToString();
        cPort.Text = _client?.LocalEndpoint.Port.ToString();
        sAddr.Text = _client?.RemoteEndpoint.Address.ToString();
        sPort.Text = _client?.RemoteEndpoint.Port.ToString();
        connectedStatus.Text = "true";
        downloadButton.Enabled = uploadButton.Enabled = deleteFileButton.Enabled = true;
    }

    string Default(string? value, string defaultValue) =>
        value == null ? defaultValue : value;
}
