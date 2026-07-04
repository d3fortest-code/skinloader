namespace WinampSkinEngine.Skin;

/// <summary>
/// Watches a .wsz file for changes and fires <see cref="SkinReloaded"/>
/// with a freshly decoded <see cref="WszSkin"/> on the calling thread
/// (use <see cref="OnChanged"/> to marshal to your render thread).
/// </summary>
public sealed class SkinHotReloader : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private string _currentPath;
    private bool _disposed;

    // Debounce: some editors write a file multiple times in rapid succession
    private DateTime _lastEvent = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Raised on a thread-pool thread when the skin file is saved.
    /// Subscriber is responsible for marshalling to the render thread.
    /// </summary>
    public event Action<WszSkin>? SkinReloaded;

    public SkinHotReloader(string wszPath)
    {
        _currentPath = Path.GetFullPath(wszPath);

        _watcher = new FileSystemWatcher
        {
            Path                  = Path.GetDirectoryName(_currentPath)!,
            Filter                = Path.GetFileName(_currentPath),
            NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents   = true
        };

        _watcher.Changed += OnFileChanged;
    }

    /// <summary>
    /// Swap to watching a different .wsz file at runtime.
    /// </summary>
    public void Watch(string wszPath)
    {
        _currentPath      = Path.GetFullPath(wszPath);
        _watcher.Path     = Path.GetDirectoryName(_currentPath)!;
        _watcher.Filter   = Path.GetFileName(_currentPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid successive writes
        var now = DateTime.UtcNow;
        if (now - _lastEvent < Debounce) return;
        _lastEvent = now;

        // Small delay so the writing process has time to release its file handle
        Thread.Sleep(150);

        try
        {
            var skin = WszSkinLoader.Load(_currentPath);
            SkinReloaded?.Invoke(skin);
        }
        catch (Exception ex)
        {
            // Log and swallow — a broken mid-save file shouldn't crash the app
            Console.Error.WriteLine($"[SkinHotReloader] Reload failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
