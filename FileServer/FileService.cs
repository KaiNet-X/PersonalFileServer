using Net.Connection.Channels;

namespace FileServer;

using Common;
using Net.Connection.Clients.Tcp;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

public class FileService
{
    private readonly string workingDirectory;
    private readonly ConcurrentDictionary<Guid, Stream> OpenFiles = new();

    private const int BufferSize = 1024 * 1024;
    
    public FileService(string workingDirectory)
    {
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
            
            foreach (var conn in ConnectionState.Connections.Where(cs => cs.User.Username == connection.User.Username && cs != connection))
                await SendTree(conn.Client, connection.User.Username);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task HandleFileRequestV2(FileRequest request, ConnectionState connection)
    {
        var requestDirectory = Path.GetDirectoryName(request.PathRequest);
        var requestFileName = Path.GetFileName(request.PathRequest);
        
        var directory = @$"{workingDirectory}\{connection.User.Username}\{requestDirectory}".PathFormat();

        if (directory.Contains("../") || directory.Contains(@"..\"))
        {
            Console.WriteLine($"Potential malicious url from {connection.Endpoint}: {requestDirectory}");
            Console.WriteLine(directory);
            return;
        }
        var filePath = @$"{directory}\{requestFileName}".PathFormat();

        try
        {
            switch (request.RequestType)
            {
                case FileRequestType.Upload:
                    await HandleUploadRequestV2Async(request, connection, directory, filePath);
                    break;
                case FileRequestType.Delete:
                    await HandleDeleteRequestV2Async(request, connection, filePath);
                    break;
                case FileRequestType.Download:
                    await HandleDownloadRequestV2Async(request, connection, filePath);
                    break;
            }

            await SendTree(connection.Client, connection.User.Username);
            
            foreach (var conn in ConnectionState.Connections.Where(cs => cs.User.Username == connection.User.Username && cs != connection))
                await SendTree(conn.Client, connection.User.Username);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void RemoveUserDirecory(string username)
    {
        var directory = @$"{workingDirectory}\{username}".PathFormat();
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
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
    
    private async Task HandleDownloadRequestV2Async(FileRequest request, ConnectionState connection, string path)
    {
        Console.WriteLine($"{connection.Endpoint} requested {request.PathRequest}");

        var channel = await connection.Client.OpenChannelAsync<TcpChannel>();
        await channel.SendBytesAsync(request.RequestId.ToByteArray());
        await using (var file = File.OpenRead(path))
        {
            await channel.SendBytesAsync(BitConverter.GetBytes(file.Length));
            
            var buffer = new byte[file.Length < BufferSize ? file.Length : BufferSize];
            while (file.Position < file.Length)
            {
                var read = await file.ReadAsync(buffer);
                await channel.SendBytesAsync(buffer.AsMemory().Slice(0, read));
            }
        }
        Console.WriteLine($"{connection.Endpoint} downloaded {request.PathRequest}");
    }
    
    private async Task HandleUploadRequestV2Async(FileRequest request, ConnectionState connection, string directory, string filePath)
    {
        Directory.CreateDirectory(directory);
        
        var channel = await connection.Client.OpenChannelAsync<TcpChannel>();
        await channel.SendBytesAsync(request.RequestId.ToByteArray());

        var buf = new byte[BufferSize];
        var read = 0;
        
        while (read < 4)
            read += await channel.ReceiveToBufferAsync(buf.AsMemory().Slice(read, 4 - read));
        
        var length = BitConverter.ToInt32(buf, 0);
        
        await using (FileStream destination = File.Create(filePath))
        {
            var totalRead = 0;
            while (totalRead < length)
            {
                read = await channel.ReceiveToBufferAsync(buf);
                totalRead += read;
                await destination.WriteAsync(buf, 0, read);
            }
        }
        
        await connection.Client.CloseChannelAsync(channel);

        Console.WriteLine($"{connection.Endpoint} uploaded {request.PathRequest}");
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
        if (Directory.Exists(filePath))
            Directory.Delete(filePath, true);
        else
            File.Delete(filePath);
        
        Console.WriteLine($"{connection.Endpoint} deleted {request.PathRequest}");
    }
    
    private async Task HandleDeleteRequestV2Async(FileRequest request, ConnectionState connection, string filePath)
    {
        if (Directory.Exists(filePath))
            Directory.Delete(filePath, true);
        else
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
