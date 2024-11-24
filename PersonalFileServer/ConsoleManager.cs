using System.Collections.Concurrent;

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

    private static void ReadNext()
    {
        
    }
    
    public static async Task<char> PromptKeyAsync(string prompt)
    {
        try
        {
            await _semaphore.WaitAsync();
            Console.WriteLine(prompt);
            return Console.ReadKey().KeyChar;
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
            return Console.ReadLine();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task<string?> GetNextCommand(int? timeout = null)
    {
        try
        {
            if (timeout.HasValue)
                await _semaphore.WaitAsync(timeout.Value);
            else
                await _semaphore.WaitAsync();

            return Console.ReadLine();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}