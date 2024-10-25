namespace FileClient;

using Common;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using Net.Connection.Clients.Tcp;
using System.Threading;

public class FileService
{
    private readonly Client client;
    private readonly AuthService authService;

    private readonly string DownloadDirectory = @$"{Directory.GetCurrentDirectory()}\Files";

    private readonly Dictionary<Guid, MemoryStream> DownloadRequests = new Dictionary<Guid, MemoryStream>();

    private readonly SemaphoreSlim downloadSemaphore = new SemaphoreSlim(1);

    public FileService(Client client, AuthService authService)
    {
        this.client = client;
        this.authService = authService;

        client.OnMessageReceived<FileRequestMessage>(msg => OnFileMessage(msg));
    }

    public async Task UploadFileAsync(string path, string name)
    {
        byte[] memBuf;

        var fileStream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan
        });

        await using (var memoryStream = new MemoryStream())
        {
            await using var compressionStream = new GZipStream(memoryStream, CompressionLevel.SmallestSize);
            await fileStream.CopyToAsync(compressionStream);
            memBuf = memoryStream.ToArray();
        }

        if (memBuf.Length > fileStream.Length)
        {
            memBuf = new byte[fileStream.Length];
            fileStream.Seek(0, SeekOrigin.Begin);
            await fileStream.ReadAsync(memBuf);
        }

        await fileStream.DisposeAsync();

        var newMsg = new FileRequestMessage() { RequestType = FileRequestType.Upload, PathRequest = name, User = authService.User, FileData = memBuf };
       
        await client.SendMessageAsync(newMsg);
    }

    private async Task OnFileMessage(FileRequestMessage msg)
    {
        Directory.CreateDirectory(DownloadDirectory);

        if (!DownloadRequests.ContainsKey(msg.RequestId))
            DownloadRequests.Add(msg.RequestId, new MemoryStream());

        var current = DownloadRequests[msg.RequestId];

        try
        {
            await downloadSemaphore.WaitAsync();
            await current.WriteAsync(msg.FileData);
        }
        catch (Exception ex)
        {

        }
        finally
        {
            downloadSemaphore.Release();
        }

        if (msg.EndOfMessage)
        {
            current.Seek(0, SeekOrigin.Begin);

            await using (var newFile = File.Create($@"{DownloadDirectory}\{msg.FileName}"))
            {
                await using (var decompress = new GZipStream(current, CompressionMode.Decompress))
                {
                    await decompress.CopyToAsync(newFile);
                }
            }
            DownloadRequests.Remove(msg.RequestId);
        }
    }
}
