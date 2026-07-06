using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinampSkinEngine.UI;
using SkinEngine.Core.Rendering;
using SkinEngine.Core;

namespace WinampSkinEngine;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        AppLogger.Init();
        AppLogger.Info("=== WinampSkinEngine Starting ===");
        AppLogger.Info($"Args: [{string.Join(", ", args)}]");

        // Flush log on any exit (including taskkill graceful, Ctrl+C, etc.)
        AppDomain.CurrentDomain.ProcessExit += (_, _) => AppLogger.Flush();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppLogger.Error($"Unhandled exception: {e.ExceptionObject}");
            AppLogger.Flush();
        };
        Application.ThreadException += (_, e) =>
        {
            AppLogger.Error("Thread exception", e.Exception);
            AppLogger.Flush();
        };

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        AppLogger.Info("WinForms initialized (HighDpi=PerMonitorV2, VisualStyles=on)");

        string? skinPath = args.Length > 0 ? args[0] : null;

        // Auto-detect .wsz file in the app directory if none specified
        if (skinPath is null)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var wszFile = Directory.GetFiles(appDir, "*.wsz").FirstOrDefault();
            if (wszFile is not null)
            {
                skinPath = wszFile;
                AppLogger.Info($"Auto-detected .wsz: {skinPath}");
            }
            else
            {
                AppLogger.Warn("No .wsz file found in app directory — running without skin");
            }
        }
        else
        {
            AppLogger.Info($"Skin path from args: {skinPath}");
            if (!File.Exists(skinPath))
                AppLogger.Warn($"Skin file does not exist: {skinPath}");
        }

        AppLogger.Info("Creating D2DSharedDevice...");
        using var sharedDevice = D2DSharedDevice.Create();
        AppLogger.Info("D2DSharedDevice created successfully");

        AppLogger.Info("Creating PlayerWindow...");
        using var window = new PlayerWindow(skinPath, sharedDevice);
        AppLogger.Info("PlayerWindow created, starting message loop...");

        Application.Run(window);

        AppLogger.Info("Message loop ended");
        AppLogger.Info("=== WinampSkinEngine Shutting Down ===");
        AppLogger.Flush();
    }
}
