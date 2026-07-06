namespace SkinEngine.Core.Skin;

/// <summary>
/// Watches a .wsz skin file for changes and automatically reloads it.
/// Uses <see cref="FileSystemWatcher"/> with debouncing to avoid rapid-fire reloads.
/// </summary>
public sealed class SkinHotReloader : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private string _currentPath;
    private bool _disposed;

    private DateTime _lastEvent = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    /// <summary>Raised when the skin file has been modified and successfully reloaded.</summary>
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

    /// <summary>Switches the watcher to monitor a different .wsz file.</summary>
    public void Watch(string wszPath)
    {
        _currentPath      = Path.GetFullPath(wszPath);
        _watcher.Path     = Path.GetDirectoryName(_currentPath)!;
        _watcher.Filter   = Path.GetFileName(_currentPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var now = DateTime.UtcNow;
        if (now - _lastEvent < Debounce) return;
        _lastEvent = now;

        Thread.Sleep(150);

        try
        {
            var skin = WszSkinLoader.Load(_currentPath);
            SkinReloaded?.Invoke(skin);
        }
        catch (Exception ex)
        {
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