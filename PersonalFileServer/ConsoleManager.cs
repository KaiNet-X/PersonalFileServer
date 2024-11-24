using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileServer;

public static class ConsoleManager
{
    private static SemaphoreSlim _semaphore = new(1);
    private static ConcurrentQueue<string> messages = new();
    private static ConcurrentQueue<string> commands = new();
    
    public static void QueueLine(string line)
    {
        messages.Enqueue(line);
        WriteAll();
    }

    private static Task QueueNextAsync()
    {
        return Task.Run(() =>
        {
            var result = Console.ReadLine();
            commands.Enqueue(result);
            return Task.CompletedTask;
        });
    }

    private static async Task WriteAll()
    {
        try
        {
            await _semaphore.WaitAsync();
            while (messages.Count > 0)
            {
                if (messages.TryDequeue(out var message))
                    Console.WriteLine(message);
            }
        }
        finally
        {
            _semaphore.Release();
        }

    }
    
    public static async Task<string> Prompt(string prompt)
    {
        try
        {
            await _semaphore.WaitAsync();
            Console.WriteLine(prompt);
            if (commands.TryDequeue(out var command))
                return command;
            await QueueNextAsync();
            commands.TryDequeue(out command);
            return command;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task<string?> GetNextCommand()
    {
        if (commands.TryDequeue(out var command))
            return command;
        
        try
        {
            await _semaphore.WaitAsync();
            await QueueNextAsync();
            commands.TryDequeue(out command);
            return command;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public static async Task<string?> GetNextCommand(int timeout)
    {
        var timer = Stopwatch.StartNew();
        if (commands.TryDequeue(out var command))
            return command;
        
        try
        {
            await _semaphore.WaitAsync(timeout);
            timer.Stop();
            if (timer.ElapsedMilliseconds > timeout)
                return null;
            
            var tasks = new []
            {
                QueueNextAsync(),
                Task.Delay(timeout - (int)timer.ElapsedMilliseconds)
            };

            await Task.WhenAny(tasks);
            commands.TryDequeue(out command);
            return command;
        }
        finally
        {
            _semaphore.Release();
        }
    }

}