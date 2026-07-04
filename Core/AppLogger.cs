namespace WinampSkinEngine.Core;

using System.IO;

/// <summary>
/// Thread-safe logger that writes each message immediately to a log file.
/// Uses a lock to ensure thread safety. Also buffers in memory for <see cref="Flush"/>.
/// </summary>
public static class AppLogger
{
    private static readonly object _lock = new();
    private static string? _logPath;
    private static StreamWriter? _writer;
    private static bool _initialized;

    public static void Init(string? explicitPath = null)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _logPath = explicitPath ?? Path.Combine(appDir, "WinampSkinEngine.log");

        lock (_lock)
        {
            _writer = new StreamWriter(_logPath, append: false) { AutoFlush = true };
            _initialized = true;
        }

        Info("=== AppLogger initialized ===");
        Info($"Log file: {_logPath}");
        Info($"OS: {Environment.OSVersion}");
        Info($"CLR: {Environment.Version}");
        Info($"64-bit: {Environment.Is64BitProcess}");
        Info($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
    }

    public static void Info(string msg)  => Write("INFO", msg);
    public static void Warn(string msg)  => Write("WARN", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    public static void Error(string msg, Exception ex)
    {
        Write("ERROR", msg);
        Write("ERROR", $"  Exception: {ex.GetType().Name}: {ex.Message}");
        if (ex.StackTrace is not null)
            Write("ERROR", $"  StackTrace: {ex.StackTrace}");
        if (ex.InnerException is not null)
            Write("ERROR", $"  Inner: {ex.InnerException.Message}");
    }

    private static void Write(string level, string msg)
    {
        lock (_lock)
        {
            if (!_initialized || _writer is null) return;
            try
            {
                _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level,-5}] {msg}");
            }
            catch
            {
                // Swallow — nothing we can do if the log write fails
            }
        }
    }

    /// <summary>
    /// Closes the log file. Called at app exit.
    /// </summary>
    public static void Flush()
    {
        lock (_lock)
        {
            if (!_initialized) return;
            _initialized = false;
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
            catch
            {
                // Swallow
            }
        }
    }
}
