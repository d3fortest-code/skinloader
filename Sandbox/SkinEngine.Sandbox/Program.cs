namespace SkinEngine.Sandbox;

using System;
using System.IO;
using System.Windows.Forms;

static class Program
{
    public static string? SkinPath { get; set; }
    public static int ScaleFactor { get; set; } = 1;

    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sandbox_settings.txt");

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        SkinEngine.Core.AppLogger.Init();

        if (args.Length > 0 && File.Exists(args[0]))
        {
            SkinPath = args[0];
        }
        else
        {
            SkinPath = LoadLastSkinPath();
        }

        if (SkinPath is null)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var wsz = Directory.GetFiles(appDir, "*.wsz");
            if (wsz.Length > 0)
                SkinPath = wsz[0];
        }

        if (SkinPath is not null)
            SkinEngine.Core.AppLogger.Info($"Using skin: {SkinPath}");
        else
            SkinEngine.Core.AppLogger.Info("No skin found — using fallback rendering");

        var selector = new DemoSelectorForm();
        Application.Run(selector);

        SkinEngine.Core.AppLogger.Flush();
    }

    public static void SaveLastSkinPath(string path)
    {
        try { File.WriteAllText(SettingsPath, path); }
        catch { }
    }

    private static string? LoadLastSkinPath()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var path = File.ReadAllText(SettingsPath).Trim();
                if (File.Exists(path))
                    return path;
            }
        }
        catch { }
        return null;
    }
}
