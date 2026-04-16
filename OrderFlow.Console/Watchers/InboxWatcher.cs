using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

namespace OrderFlow.Console.Watchers;

public class InboxWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly OrderPipeline _pipeline;
    private readonly OrderRepository _repository = new();
    private readonly SemaphoreSlim _semaphore = new(2);
    private readonly HashSet<string> _processing = new();
    private readonly object _lock = new();

    public InboxWatcher(string inboxPath, OrderPipeline pipeline)
    {
        _pipeline = pipeline;
        Directory.CreateDirectory(inboxPath);
        Directory.CreateDirectory(Path.Combine(inboxPath, "processed"));
        Directory.CreateDirectory(Path.Combine(inboxPath, "failed"));

        _watcher = new FileSystemWatcher(inboxPath, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        System.Console.WriteLine($"[WATCHER] Watching: {inboxPath}");
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            if (_processing.Contains(e.FullPath)) return;
            _processing.Add(e.FullPath);
        }

        Task.Run(() => ProcessFileAsync(e.FullPath));
    }

    private async Task ProcessFileAsync(string filePath)
    {
        await _semaphore.WaitAsync();
        try
        {
            System.Console.WriteLine($"[WATCHER] Detected: {Path.GetFileName(filePath)}");

            await RetryReadAsync(filePath);

            var orders = await _repository.LoadFromJsonAsync(filePath);
            if (orders.Count == 0)
            {
                System.Console.WriteLine($"[WATCHER] No orders found in {Path.GetFileName(filePath)}");
                return;
            }

            System.Console.WriteLine($"[WATCHER] Importing {orders.Count} order(s) from {Path.GetFileName(filePath)}");

            foreach (var order in orders)
                _pipeline.ProcessOrder(order);

            var dest = Path.Combine(
                Path.GetDirectoryName(filePath)!,
                "processed",
                Path.GetFileName(filePath)
            );
            File.Move(filePath, dest, overwrite: true);
            System.Console.WriteLine($"[WATCHER] Moved to processed/: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[WATCHER] ERROR: {ex.Message}");
            try
            {
                var failedDir = Path.Combine(Path.GetDirectoryName(filePath)!, "failed");
                var dest = Path.Combine(failedDir, Path.GetFileName(filePath));
                File.Move(filePath, dest, overwrite: true);
                await File.WriteAllTextAsync(dest + ".error.txt", ex.ToString());
                System.Console.WriteLine($"[WATCHER] Moved to failed/: {Path.GetFileName(filePath)}");
            }
            catch { /* ignoruj błędy przenoszenia */ }
        }
        finally
        {
            lock (_lock) _processing.Remove(filePath);
            _semaphore.Release();
        }
    }

    private static async Task RetryReadAsync(string path, int retries = 5, int delayMs = 300)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return;
            }
            catch (IOException)
            {
                if (i == retries - 1) throw;
                await Task.Delay(delayMs);
            }
        }
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _semaphore.Dispose();
    }
}