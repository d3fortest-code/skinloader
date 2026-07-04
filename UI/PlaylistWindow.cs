namespace WinampSkinEngine.UI;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using WinampSkinEngine.Rendering;
using WinampSkinEngine.Skin;
using WinampSkinEngine.Core;

/// <summary>
/// Playlist window. Renders pledit.bmp chrome tiles with a scrollable
/// track list drawn via DirectWrite. Resizable vertically.
/// </summary>
public sealed class PlaylistWindow : Form
{
    private readonly D2DSharedDevice _sharedDevice;
    private D2DWindowSurface? _surface;
    private SkinAtlas? _atlas;
    private SkinRenderer? _renderer;

    private System.Threading.Timer? _frameTimer;
    private readonly object _renderLock = new();

    private const int MIN_HEIGHT = 116;
    private const int LINE_HEIGHT = 14;

    public List<string> Tracks
    {
        get => _renderer?.Tracks ?? new();
        set { if (_renderer is not null) _renderer.Tracks = value; }
    }

    public PlaylistWindow(D2DSharedDevice sharedDevice, SkinAtlas atlas, SkinDefinition skinDef, Vortice.DirectWrite.IDWriteFactory dwFactory)
    {
        _sharedDevice = sharedDevice;
        AppLogger.Info("[PlaylistWindow] Constructing...");

        Text            = "Winamp Playlist";
        ClientSize      = new Size(275, 232);
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = Color.Black;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint            |
                 ControlStyles.Opaque, true);
        UpdateStyles();

        MouseDown += OnMouseDown;

        Load += (_, _) =>
        {
            AppLogger.Info("[PlaylistWindow] Load event — creating surface and renderer...");
            _surface  = _sharedDevice.CreateWindowSurface(Handle, ClientSize.Width, ClientSize.Height);
            _atlas    = atlas;
            _renderer = new SkinRenderer(_surface, _atlas, skinDef, dwFactory);

            AppLogger.Info("[PlaylistWindow] Starting render timer...");
            _frameTimer = new System.Threading.Timer(_ => RenderFrame(), null, 0, 16);
        };

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                AppLogger.Info("[PlaylistWindow] Close requested — hiding instead");
                Hide();
                e.Cancel = true;
            }
        };

        AppLogger.Info("[PlaylistWindow] Constructor done");
    }

    public void Toggle()
    {
        AppLogger.Info($"[PlaylistWindow] Toggle — Visible was {Visible}, now {!Visible}");
        Visible = !Visible;
    }

    public void SetPlaylistConfig(PleditConfig? config)
    {
        lock (_renderLock)
        {
            if (_renderer is not null)
                _renderer.PlaylistConfig = config;
        }
    }

    public void Show(Form owner)
    {
        Owner = owner;
        Show();
    }

    public new void Show()
    {
        if (Owner is not null)
        {
            Location = new Point(Owner.Left, Owner.Top + 232);
        }
        else
        {
            Location = new Point(100, 100 + 232);
        }
        AppLogger.Info($"[PlaylistWindow] Showing at ({Location.X}, {Location.Y})");
        base.Show(Owner);
    }

    private void RenderFrame()
    {
        lock (_renderLock)
        {
            if (_surface is null || _renderer is null) return;
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            _surface.BeginDraw();
            _renderer.DrawPlaylistWindow(ClientSize.Width, ClientSize.Height);
            _surface.EndDraw();
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_SIZING    = 0x0214;
        const int WM_NCHITTEST = 0x0084;
        const int HTBOTTOM     = 15;

        if (m.Msg == WM_SIZING)
        {
            var rc = Marshal.PtrToStructure<RECT>(m.LParam);
            int newHeight = rc.Bottom - rc.Top;
            if (newHeight < MIN_HEIGHT)
            {
                rc.Bottom = rc.Top + MIN_HEIGHT;
                Marshal.StructureToPtr(rc, m.LParam, true);
            }
            m.Result = new IntPtr(1);
            return;
        }

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);

            int y = (int)m.LParam & 0xFFFF;
            int screenY = y >> 16;
            int clientBottom = PointToScreen(new Point(0, ClientSize.Height)).Y;
            if (screenY >= clientBottom - 4 && screenY <= clientBottom)
            {
                m.Result = new IntPtr(HTBOTTOM);
                return;
            }
            return;
        }

        base.WndProc(ref m);
    }

    private struct RECT { public int Left, Top, Right, Bottom; }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        lock (_renderLock)
        {
            if (ClientSize.Width > 0 && ClientSize.Height > 0)
                _surface?.Resize(ClientSize.Width, ClientSize.Height);
        }
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

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION        = 0x2;

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(
        IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
}
