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

    private readonly string DownloadDirectory = @$"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Files";

    private readonly Dictionary<Guid, MemoryStream> DownloadRequests = new Dictionary<Guid, MemoryStream>();

    private readonly SemaphoreSlim downloadSemaphore = new SemaphoreSlim(1);

    public FileService(Client client, AuthService authService)
    {
        this.client = client;
        this.authService = authService;

        client.OnMessageReceived<FileRequestMessage>(OnFileMessage);
    }

    public async Task UploadFileAsync(string path, string name)
    {
        byte[] memBuf;

        await using (var fileStream = new FileStream(path, new FileStreamOptions
                     {
                         Access = FileAccess.Read,
                         Mode = FileMode.Open,
                         Options = FileOptions.SequentialScan
                     }))
        {
            memBuf = await Crypto.CompressAsync(fileStream);
        }
        
        memBuf = await Crypto.EncryptAESAsync(memBuf, authService.EncKey, authService.IV);
        
        var newMsg = new FileRequestMessage { RequestType = FileRequestType.Upload, PathRequest = name, FileData = memBuf };
        
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
            
            var buf = await Crypto.DecryptAESAsync(current, authService.EncKey, authService.IV);
            
            buf = await Crypto.DecompressAsync(buf);

            await using (var fileStream =
                         File.Create($@"{DownloadDirectory}{Path.DirectorySeparatorChar}{msg.FileName}"))
            {
                await using (var memoryStream = new MemoryStream(buf))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }
            }
            
            DownloadRequests.Remove(msg.RequestId);
        }
    }
}
