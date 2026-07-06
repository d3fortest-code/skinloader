namespace WinampSkinEngine.UI;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using SkinEngine.Core.Rendering;
using SkinEngine.Core.Skin;
using SkinEngine.Core;

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

    // ── Scale ─────────────────────────────────────────────────────────────────
    private const int BASE_W = 275;
    private const int BASE_H = 116;
    public int ScaleFactor { get; private set; } = 1;
    private ToolStripMenuItem? _scale1x, _scale2x, _scale3x, _scale4x;

    // ── Saved skin config (re-applied on scale change) ────────────────────────
    private PleditConfig? _savedPleditConfig;
    private uint[]? _savedVisColors;

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
        _renderer.FreezeTimeDisplay = true;
        _renderer.FreezeSeekBar = true;
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

        _eqWindow.SetEqBands(_renderer.EqBands, _renderer.EqPreamp);

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
                    _savedPleditConfig = skin.PleditConfig;
                    AppLogger.Info("[PlayerWindow] Applied pledit.txt config to renderer");
                }

                // Pass vis colors to the renderer
                if (skin.VisColors is not null && _renderer is not null)
                {
                    _renderer.VisColors = skin.VisColors;
                    _savedVisColors = skin.VisColors;
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

        var scaleMenu = new ToolStripMenuItem("Scale");
        _scale1x = new ToolStripMenuItem("1x (275×116)", null, (_, _) => SetScale(1));
        _scale2x = new ToolStripMenuItem("2x (550×232)", null, (_, _) => SetScale(2));
        _scale3x = new ToolStripMenuItem("3x (825×348)", null, (_, _) => SetScale(3));
        _scale4x = new ToolStripMenuItem("4x (1100×464)", null, (_, _) => SetScale(4));
        _scale1x.Checked = true;
        scaleMenu.DropDownItems.AddRange([_scale1x, _scale2x, _scale3x, _scale4x]);
        menu.Items.Add(scaleMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit",      null, (_, _) => Application.Exit());
        ContextMenuStrip = menu;
        AppLogger.Info("[PlayerWindow] Context menu created (Equalizer / Playlist / Scale / About / Exit)");
    }

    private void SetScale(int scale)
    {
        if (scale < 1 || scale > 4 || scale == ScaleFactor) return;
        ScaleFactor = scale;

        AppLogger.Info($"[PlayerWindow] Setting scale to {scale}x — destroying and recreating surfaces...");

        _scale1x!.Checked = scale == 1;
        _scale2x!.Checked = scale == 2;
        _scale3x!.Checked = scale == 3;
        _scale4x!.Checked = scale == 4;

        // 1. Stop render timer
        _frameTimer?.Dispose();
        _frameTimer = null;

        // 2. Save visibility state and close sub-windows
        bool wasEqVisible = _eqWindow?.Visible == true;
        bool wasPlVisible = _playlistWindow?.Visible == true;
        _eqWindow?.Close();
        _playlistWindow?.Close();
        _aboutWindow?.Close();

        // 3. Dispose main surface
        lock (_renderLock)
        {
            _surface?.Dispose();
            _surface = null;
        }

        // 4. Resize main window (triggers OnResize, but surface is null so it's safe)
        SuspendLayout();
        ClientSize = new System.Drawing.Size(BASE_W * scale, BASE_H * scale);
        ResumeLayout(false);

        // 5. Recreate main surface at new size
        lock (_renderLock)
        {
            _surface = _sharedDevice.CreateWindowSurface(Handle, ClientSize.Width, ClientSize.Height);
            // Renderer still uses old atlas — swap its surface reference
            _renderer = new SkinRenderer(_surface, _atlas!, _skinDef!, _sharedDevice.DWriteFactory);
            _renderer.FreezeTimeDisplay = true;
            _renderer.FreezeSeekBar = true;
        }

        // 6. Re-create sub-windows (their Load events create new surfaces at new sizes)
        _eqWindow = new EqWindow(_sharedDevice, _atlas!, _skinDef!, _sharedDevice.DWriteFactory) { Owner = this };
        _playlistWindow = new PlaylistWindow(_sharedDevice, _atlas!, _skinDef!, _sharedDevice.DWriteFactory) { Owner = this };
        _aboutWindow = new AboutWindow(_sharedDevice) { Owner = this };

        WindowManager.Eq = _eqWindow;
        WindowManager.Playlist = _playlistWindow;

        if (wasEqVisible) _eqWindow.Show(this);
        if (wasPlVisible) _playlistWindow.Show(this);

        // 7. Re-populate state on the new renderer
        lock (_renderLock)
        {
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

            if (_savedPleditConfig is not null)
            {
                _renderer.PlaylistConfig = _savedPleditConfig;
                _eqWindow.SetPlaylistConfig(_savedPleditConfig);
                _playlistWindow.SetPlaylistConfig(_savedPleditConfig);
            }
            if (_savedVisColors is not null)
            {
                _renderer.VisColors = _savedVisColors;
                _eqWindow.SetVisColors(_savedVisColors);
            }

            _renderer.EqBands[0] = 0.6f;
            _renderer.EqBands[1] = 0.55f;
            _renderer.EqBands[2] = 0.5f;
            _renderer.EqBands[3] = 0.45f;
            _renderer.EqBands[4] = 0.4f;
            _renderer.EqBands[5] = 0.45f;
            _renderer.EqBands[6] = 0.55f;
            _renderer.EqBands[7] = 0.65f;
            _renderer.EqBands[8] = 0.7f;
            _renderer.EqBands[9] = 0.75f;

            _eqWindow.SetEqBands(_renderer.EqBands, _renderer.EqPreamp);
        }

        // 8. Restart render timer
        _frameTimer = new System.Threading.Timer(_ => RenderFrame(), null, 0, 16);
        AppLogger.Info($"[PlayerWindow] Scale to {scale}x complete — surfaces recreated");
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
            if (!_renderer.FreezeTimeDisplay)
                _renderer.ElapsedSeconds = (int)totalSeconds % 600;
            if (!_renderer.FreezeSeekBar)
                _renderer.PlaybackPosition = (totalSeconds % 60.0) / 60.0;
            _renderer.IsPlaying        = true;
            _renderer.TitleBarActive   = ContainsFocus;

            _surface.BeginDraw();
            if (ScaleFactor > 1)
                _surface.DeviceContext.Transform = System.Numerics.Matrix3x2.CreateScale((float)ScaleFactor, (float)ScaleFactor);
            _renderer.DrawMainWindow();
            if (ScaleFactor > 1)
                _surface.DeviceContext.Transform = System.Numerics.Matrix3x2.Identity;
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
