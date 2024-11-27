using System.Collections.Concurrent;

namespace Common;

public class EventQueue : IDisposable
{
    private readonly ConcurrentQueue<Func<Task>> asyncQueue = new ();
    
    private TaskCompletionSource tcs = new ();
    private CancellationTokenSource cancellationTokenSource = new();
    
    public void Enqueue(Func<Task> asyncAction)
    {
        asyncQueue.Enqueue(asyncAction);
        tcs.TrySetResult();
    }

    public EventQueue()
    {
        Task.Factory.StartNew(async () =>
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                await tcs.Task;
                
                while (asyncQueue.TryDequeue(out var task))
                {
                    try
                    {
                        await task();
                    }
                    catch { }
                }
                
                tcs = new TaskCompletionSource();
                if (!asyncQueue.IsEmpty)
                    tcs.TrySetResult();
            }
        }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
    }
}