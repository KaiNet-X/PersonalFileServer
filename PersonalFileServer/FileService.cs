namespace FileServer;

using Common;
using Net.Connection.Clients.Tcp;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

public class FileService
{
    private readonly AuthService authService;
    private readonly string workingDirectory;
    private readonly ConcurrentDictionary<Guid, Stream> OpenFiles = new();

    public FileService(AuthService authService, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(authService);

        this.authService = authService;
        this.workingDirectory = workingDirectory;
    }

    public async Task SendFile(Stream file, ServerClient client, FileRequestMessage msg)
    {
        const int sendChunkSize = 16384;

        var fileName = msg.PathRequest.PathFormat().Split(Path.DirectorySeparatorChar)[^1];

        var id = Guid.NewGuid();

        byte[] bytes = new byte[file.Length <= sendChunkSize ? file.Length : sendChunkSize];
        var max = Math.Ceiling(((float)file.Length) / (float)sendChunkSize);

        for (int i = 0; i < max; i++)
        {
            var eom = i == max - 1;

            var newMsg = new FileRequestMessage
            {
                RequestType = FileRequestType.Upload,
                PathRequest = fileName,
                RequestId = id,
                EndOfMessage = eom,
                FileData = eom ? (i > 0 ? new byte[file.Length - file.Position] : bytes) : bytes
            };

            await file.ReadAsync(newMsg.FileData);
            await client.SendMessageAsync(newMsg);
        }
    }

    public async Task HandleFileRequest(FileRequestMessage request, ConnectionState connection)
    {
        var directory = @$"{workingDirectory}\{connection.User.Username}\{request.Directory}".PathFormat();

        if (directory.Contains("../") || directory.Contains(@"..\"))
        {
            Console.WriteLine($"Potential malicious url from {connection.Endpoint}: {request.Directory}");
            Console.WriteLine(directory);
            return;
        }
        var filePath = @$"{directory}\{request.FileName}".PathFormat();

        try
        {
            switch (request.RequestType)
            {
                case FileRequestType.Download:
                    await HandleDownloadRequestAsync(request, connection, filePath);
                    break;
                case FileRequestType.Upload:
                    await HandleUploadRequestAsync(request, connection, directory, filePath);
                    break;
                case FileRequestType.Delete:
                    await HandleDeleteRequestAsync(request, connection, filePath);
                    break;
                case FileRequestType.Tree:
                    {
                        Directory.CreateDirectory(directory);
                    }
                    break;
            }
            await SendTree(connection.Client, connection.User.Username);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task HandleDownloadRequestAsync(FileRequestMessage request, ConnectionState connection, string path)
    {
        Console.WriteLine($"{connection.Endpoint} requested {request.PathRequest}");
        await using (var file = File.OpenRead(path))
        {
            await SendFile(file, connection.Client, request);
        }
        Console.WriteLine($"{connection.Endpoint} downloaded {request.PathRequest}");
    }

    private async Task HandleUploadRequestAsync(FileRequestMessage request, ConnectionState connection, string directory, string filePath)
    {
        Directory.CreateDirectory(directory);
        await using (FileStream destination = File.Create(filePath))
        {
            await destination.WriteAsync(request.FileData);
        }
        Console.WriteLine($"{connection.Endpoint} uploaded {request.PathRequest}");
    }

    private async Task HandleDeleteRequestAsync(FileRequestMessage request, ConnectionState connection, string filePath)
    {
        File.Delete(filePath);                             
        Console.WriteLine($"{connection.Endpoint} deleted {request.PathRequest}");
        
    }
    
    private Tree GetTree(string dir)
    {
        dir = dir.PathFormat();
        var tree = new Tree
        {
            Nodes = new List<Tree>()
        };

        foreach (var file in Directory.EnumerateFiles(dir))
            tree.Nodes.Add(new Tree { Value = file.Split(Path.DirectorySeparatorChar)[^1].Replace(".aes", "") });

        foreach (var folder in Directory.EnumerateDirectories(dir))
        {
            var tr = GetTree(folder);
            tr.Value = folder.Split(Path.DirectorySeparatorChar)[^1];
            tree.Nodes.Add(tr);
        }
        return tree;
    }

    private Task SendTree(ServerClient c, string username) {
        var tree = GetTree(@$"{workingDirectory}\{username}".PathFormat());
        return c.SendObjectAsync(tree);
    }
}
