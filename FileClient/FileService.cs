using System.Collections.Concurrent;
using System.Linq;
using Avalonia.Platform.Storage;
using FileClient.Views;
using Net.Connection.Channels;

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

    private readonly string DownloadDirectory = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Files";
    
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TcpChannel>> requests = new();
    
    private const int BufferSize = 1024 * 1024;
    
    public FileService(Client client, AuthService authService)
    {
        this.client = client;
        this.authService = authService;

        client.OnChannel<TcpChannel>(OnChannelOpened);
    }

    private async Task OnChannelOpened(TcpChannel channel)
    {
        try
        {
            var guidBuff = new byte[16];
            var read = 0;
            
            while (read < guidBuff.Length)
                read += await channel.ReceiveToBufferAsync(guidBuff.AsMemory().Slice(read));
            
            var reqId = new Guid(guidBuff);

            if (!requests.TryGetValue(reqId, out var request))
                await client.CloseChannelAsync(channel);
            else
                request.SetResult(channel);
        }
        catch
        {
            await client.CloseChannelAsync(channel);
        }
    }
    
    private bool TryAddRequest(Guid requestId, out TaskCompletionSource<TcpChannel> tcs)
    {
        tcs = new TaskCompletionSource<TcpChannel>();
        return requests.TryAdd(requestId, tcs);
    }

    private async Task<TcpChannel> GetChannelAsync(Guid requestId, TaskCompletionSource<TcpChannel> tcs)
    {
        var channel = await tcs.Task;
        requests.TryRemove(requestId, out _);
        return channel;
    }

    private async Task<TcpChannel> SendRequestAsync(FileRequest request)
    {
        if (!TryAddRequest(request.RequestId, out var tcs)) 
            return null;
        
        await client.SendObjectAsync(request);
        
        var channel = await GetChannelAsync(request.RequestId, tcs);

        return channel;
    }
    
    // TODO: Close channel and request after timeout
    public async Task UploadFileAsync(IStorageFile file, string path = null)
    {
        var request = new FileRequest(
            FileRequestType.Upload, 
            Guid.NewGuid(),
            path is not null ? $"{path}{Path.DirectorySeparatorChar}{file.Name}" : file.Name);

        var channel = await SendRequestAsync(request);
        if (channel is null) return;
        
        byte[] memBuf;

        await using (var fileStream = await file.OpenReadAsync())
            memBuf = await Crypto.CompressAsync(fileStream);
        
        memBuf = await Crypto.EncryptAESAsync(memBuf, authService.EncKey, authService.IV);

        await channel.SendBytesAsync(BitConverter.GetBytes(memBuf.Length));
        await channel.SendBytesAsync(memBuf);
    }

    public async Task UploadFolderAsync(IStorageFolder folder, string path = null)
    {
        var name = folder.Name;
        path = path is not null ? $"{path}{Path.DirectorySeparatorChar}{name}" : name;
        
        await foreach (var storageItem in folder.GetItemsAsync())
        {
            if (storageItem is IStorageFolder storageFolder)
                await UploadFolderAsync(storageFolder, path);
            else if (storageItem is IStorageFile storageFile)
                await UploadFileAsync(storageFile, path);
        }
    }

    public async Task DownloadFolderAsync(Node root, string path)
    {
        foreach (var node in root.SubNodes)
        {
            var subPath = $"{path}{Path.DirectorySeparatorChar}{node.Title}";
            if (node.SubNodes.Any())
                await DownloadFolderAsync(node, subPath);
            else await DownloadFileAsync(subPath);
        }
    }
    
    public async Task DownloadFileAsync(string path)
    {
        var request = new FileRequest(
            FileRequestType.Download, 
            Guid.NewGuid(),
            path);

        var channel = await SendRequestAsync(request);
        if (channel is null) return;

        var buffer = new byte[8];

        var read = 0;
        while (read < 8)
            read = await channel.ReceiveToBufferAsync(buffer.AsMemory().Slice(read, 8 - read));
        
        var length = BitConverter.ToInt64(buffer, 0);
        buffer = new byte[length];
        await using (var memory = new MemoryStream(buffer))
            
        {
            while (memory.Position < length)
            {
                var bytes = await channel.ReceiveBytesAsync();
                memory.WriteAsync(bytes);
            }
            
            await client.CloseChannelAsync(channel);
            memory.Seek(0, SeekOrigin.Begin);
            buffer = await Crypto.DecryptAESAsync(memory, authService.EncKey, authService.IV);
        }
        
        buffer = await Crypto.DecompressAsync(buffer);
        var filePath = $@"{DownloadDirectory}{Path.DirectorySeparatorChar}{path}";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        await using (var fileStream = File.Create(filePath))
            await using (var memory = new MemoryStream(buffer))
                await memory.CopyToAsync(fileStream);
    }
}
