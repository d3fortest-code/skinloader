namespace WinampSkinEngine.UI;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using WinampSkinEngine.Rendering;
using WinampSkinEngine.Skin;
using WinampSkinEngine.Core;

/// <summary>
/// Bare WinForms <see cref="Form"/> used purely as a window handle and message pump.
/// Every visible pixel is drawn by the Direct2D render path — WinForms paints nothing.
/// </summary>
public sealed class PlayerWindow : Form
{
    private readonly D2DSharedDevice _sharedDevice;
    private D2DWindowSurface? _surface;
    private SkinAtlas?          _atlas;
    private SkinRenderer?       _renderer;
    private SkinDefinition?     _skinDef;

    private EqWindow?       _eqWindow;
    private PlaylistWindow? _playlistWindow;
    private AboutWindow?    _aboutWindow;

    private SkinHotReloader? _reloader;

    private System.Threading.Timer? _frameTimer;
    private readonly object          _renderLock = new();
    private volatile WszSkin? _pendingSkin;
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private int _frameCount;

    // ── Win32 region support ─────────────────────────────────────────────────
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreatePolygonRgn(
        POINT[] lpPoint, int nCount, int nPolyFillMode);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private const int ALTERNATE = 1;

    public PlayerWindow(string? wszPath, D2DSharedDevice? sharedDevice = null)
    {
        AppLogger.Info("[PlayerWindow] Constructing...");
        _sharedDevice = sharedDevice ?? D2DSharedDevice.Create();

        Text            = "WinampSkinEngine";
        ClientSize      = new System.Drawing.Size(275, 116);
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = System.Drawing.Color.Black;

        MouseDown += OnMouseDown;
        KeyDown   += OnKeyDown;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint            |
                 ControlStyles.Opaque, true);
        UpdateStyles();

        Load += (_, _) => InitializeD2D(wszPath);
        FormClosed += (_, _) => Cleanup();

        SetupContextMenu();
        AppLogger.Info("[PlayerWindow] Constructor done");
    }

    private void InitializeD2D(string? wszPath)
    {
        AppLogger.Info("[PlayerWindow] InitializeD2D — creating surface, atlas, renderer...");

        _surface  = _sharedDevice.CreateWindowSurface(Handle, ClientSize.Width, ClientSize.Height);
        _atlas    = new SkinAtlas(_surface.DeviceContext);
        _skinDef  = SkinDefinition.BuildClassicSilver();
        _renderer = new SkinRenderer(_surface, _atlas, _skinDef, _sharedDevice.DWriteFactory);
        AppLogger.Info("[PlayerWindow] Core rendering objects created");

        if (wszPath is not null && File.Exists(wszPath))
        {
            LoadSkin(wszPath);
            SetupHotReload(wszPath);
        }
        else
        {
            AppLogger.Warn($"[PlayerWindow] No skin loaded — path: '{wszPath}'");
        }

        AppLogger.Info("[PlayerWindow] Creating EQ window...");
        _eqWindow = new EqWindow(_sharedDevice, _atlas, _skinDef, _sharedDevice.DWriteFactory) { Owner = this };

        AppLogger.Info("[PlayerWindow] Creating Playlist window...");
        _playlistWindow = new PlaylistWindow(_sharedDevice, _atlas, _skinDef, _sharedDevice.DWriteFactory) { Owner = this };

        AppLogger.Info("[PlayerWindow] Creating About window...");
        _aboutWindow = new AboutWindow(_sharedDevice) { Owner = this };

        WindowManager.Main = this;
        WindowManager.Eq   = _eqWindow;
        WindowManager.Playlist = _playlistWindow;

        AppLogger.Info("[PlayerWindow] Showing EQ and Playlist windows...");
        _eqWindow.Show(this);
        _playlistWindow.Show(this);

        _eqWindow.Location       = new System.Drawing.Point(Left, Top + 116);
        _playlistWindow.Location = new System.Drawing.Point(Left, Top + 232);

        _clock.Start();
        AppLogger.Info("[PlayerWindow] Starting 60fps render timer...");

        // Populate demo tracks for the playlist
        _renderer.Tracks = new List<string>
        {
            "1. DJ Mike Llama - Llama Whippin' Intro",
            "2. Darude - Sandstorm",
            "3. Eiffel 65 - Blue (Da Ba Dee)",
            "4. Vengaboys - We Like to Party",
            "5. Lou Bega - Mambo No. 5",
            "6. Aqua - Barbie Girl",
            "7. Scatman John - Scatman",
            "8. Haddaway - What Is Love",
            "9. Corona - Rhythm of the Night",
            "10. Real McCoy - Another Night",
            "11. La Bouche - Be My Lover",
            "12. Culture Beat - Mr. Vain"
        };
        _playlistWindow.Tracks = _renderer.Tracks;

        // Set demo EQ bands (V-shape like the real Winamp default)
        _renderer.EqBands[0] = 0.6f;  // 60Hz
        _renderer.EqBands[1] = 0.55f; // 170Hz
        _renderer.EqBands[2] = 0.5f;  // 310Hz
        _renderer.EqBands[3] = 0.45f; // 600Hz
        _renderer.EqBands[4] = 0.4f;  // 1kHz
        _renderer.EqBands[5] = 0.45f; // 3kHz
        _renderer.EqBands[6] = 0.55f; // 6kHz
        _renderer.EqBands[7] = 0.65f; // 12kHz
        _renderer.EqBands[8] = 0.7f;  // 14kHz
        _renderer.EqBands[9] = 0.75f; // 16kHz

        _frameTimer = new System.Threading.Timer(_ => RenderFrame(), null, 0, 16);
    }

    private void LoadSkin(string wszPath)
    {
        try
        {
            AppLogger.Info($"[PlayerWindow] Loading skin from: {wszPath}");
            var skin = WszSkinLoader.Load(wszPath);

            bool hasMain = skin.MainBitmap is not null;
            bool hasEq   = skin.EqMain is not null;
            bool hasPl   = skin.PlEdit is not null;
            AppLogger.Info($"[PlayerWindow] Skin decoded — Main={hasMain}, EqMain={hasEq}, PlEdit={hasPl}");

            lock (_renderLock)
            {
                _atlas!.UploadSkin(skin);

                // Pass playlist config to the renderer
                if (skin.PleditConfig is not null && _renderer is not null)
                {
                    _renderer.PlaylistConfig = skin.PleditConfig;
                    AppLogger.Info("[PlayerWindow] Applied pledit.txt config to renderer");
                }

                // Pass vis colors to the renderer
                if (skin.VisColors is not null && _renderer is not null)
                {
                    _renderer.VisColors = skin.VisColors;
                }

                // Pass playlist config to EQ and Playlist renderers too
                _eqWindow?.SetPlaylistConfig(skin.PleditConfig);
                _playlistWindow?.SetPlaylistConfig(skin.PleditConfig);
                _eqWindow?.SetVisColors(skin.VisColors);
            }

            // Apply window regions from region.txt
            if (skin.Regions is not null)
            {
                ApplyWindowRegions(skin.Regions);
            }

            AppLogger.Info("[PlayerWindow] Atlas upload complete");
            skin.Dispose();
        }
        catch (Exception ex)
        {
            AppLogger.Error("[PlayerWindow] Failed to load skin", ex);
        }
    }

    private void ApplyWindowRegions(Dictionary<string, List<System.Drawing.Point>> regions)
    {
        if (regions.TryGetValue("normal", out var mainRegion))
            ApplyRegion(Handle, mainRegion);

        if (regions.TryGetValue("equalizer", out var eqRegion) && _eqWindow?.IsHandleCreated == true)
            ApplyRegion(_eqWindow.Handle, eqRegion);

        if (regions.TryGetValue("playlist", out var plRegion) && _playlistWindow?.IsHandleCreated == true)
            ApplyRegion(_playlistWindow.Handle, plRegion);
    }

    private static void ApplyRegion(IntPtr hwnd, List<System.Drawing.Point> points)
    {
        if (points.Count < 3) return;

        var pts = new POINT[points.Count];
        for (int i = 0; i < points.Count; i++)
            pts[i] = new POINT { X = points[i].X, Y = points[i].Y };

        IntPtr hRgn = CreatePolygonRgn(pts, points.Count, ALTERNATE);
        if (hRgn != IntPtr.Zero)
        {
            SetWindowRgn(hwnd, hRgn, true);
            // Note: After SetWindowRgn, the system owns the region — do NOT delete it.
            AppLogger.Info($"[PlayerWindow] Applied region with {points.Count} points to hwnd 0x{hwnd:X}");
        }
    }

    private void SetupHotReload(string wszPath)
    {
        AppLogger.Info($"[PlayerWindow] Setting up hot-reload watcher for: {wszPath}");
        _reloader = new SkinHotReloader(wszPath);
        _reloader.SkinReloaded += skin =>
        {
            AppLogger.Info("[PlayerWindow] Hot-reload triggered — queuing skin swap");
            _pendingSkin = skin;
        };
    }

    private void SetupContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Equalizer", null, (_, _) => _eqWindow?.Toggle());
        menu.Items.Add("Playlist",  null, (_, _) => _playlistWindow?.Toggle());
        menu.Items.Add("About",     null, (_, _) => _aboutWindow?.Show());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit",      null, (_, _) => Application.Exit());
        ContextMenuStrip = menu;
        AppLogger.Info("[PlayerWindow] Context menu created (Equalizer / Playlist / About / Exit)");
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Alt)
        {
            switch (e.KeyCode)
            {
                case Keys.G:
                    AppLogger.Info("[PlayerWindow] Alt+G pressed — toggling EQ");
                    _eqWindow?.Toggle();
                    e.Handled = true;
                    break;
                case Keys.E:
                    AppLogger.Info("[PlayerWindow] Alt+E pressed — toggling Playlist");
                    _playlistWindow?.Toggle();
                    e.Handled = true;
                    break;
                case Keys.W:
                    AppLogger.Info("[PlayerWindow] Alt+W pressed");
                    e.Handled = true;
                    break;
            }
        }
    }

    private void RenderFrame()
    {
        lock (_renderLock)
        {
            if (_surface is null || _renderer is null) return;

            var pending = _pendingSkin;
            if (pending is not null)
            {
                AppLogger.Info("[PlayerWindow] Applying hot-reloaded skin...");
                _pendingSkin = null;
                _atlas!.DisposeAll();
                _atlas.UploadSkin(pending);

                // Re-apply playlist config on hot-reload
                if (pending.PleditConfig is not null)
                {
                    _renderer.PlaylistConfig = pending.PleditConfig;
                    _eqWindow?.SetPlaylistConfig(pending.PleditConfig);
                    _playlistWindow?.SetPlaylistConfig(pending.PleditConfig);
                }

                // Re-apply vis colors on hot-reload
                if (pending.VisColors is not null)
                {
                    _renderer.VisColors = pending.VisColors;
                    _eqWindow?.SetVisColors(pending.VisColors);
                }

                // Re-apply window regions on hot-reload
                if (pending.Regions is not null)
                    ApplyWindowRegions(pending.Regions);

                pending.Dispose();
            }

            double totalSeconds = _clock.Elapsed.TotalSeconds;
            _renderer.ElapsedSeconds   = (int)totalSeconds % 600;
            _renderer.PlaybackPosition = (totalSeconds % 60.0) / 60.0;
            _renderer.IsPlaying        = true;
            _renderer.TitleBarActive   = ContainsFocus;

            _surface.BeginDraw();
            _renderer.DrawMainWindow();
            _surface.EndDraw();

            _frameCount++;
            if (_frameCount % 300 == 0)
                AppLogger.Info($"[PlayerWindow] Rendered {_frameCount} frames ({_clock.Elapsed.TotalSeconds:F1}s elapsed)");
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        lock (_renderLock)
        {
            _surface?.Resize(ClientSize.Width, ClientSize.Height);
        }
    }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        WindowManager.OnMainMoved();
    }

    protected override void OnPaint(PaintEventArgs e) { }
    protected override void OnPaintBackground(PaintEventArgs e) { }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
        }
    }

    private void Cleanup()
    {
        AppLogger.Info("[PlayerWindow] Cleaning up...");
        _frameTimer?.Dispose();
        _reloader?.Dispose();

        _eqWindow?.Close();
        _playlistWindow?.Close();
        _aboutWindow?.Close();

        lock (_renderLock)
        {
            _renderer = null;
            _surface?.Dispose();
            _atlas?.Dispose();
        }

        WindowManager.Main = null;
        WindowManager.Eq   = null;
        WindowManager.Playlist = null;

        AppLogger.Info("[PlayerWindow] Cleanup complete");
    }

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION        = 0x2;

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(
        IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
}
