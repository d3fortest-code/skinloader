namespace WinampSkinEngine.UI;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using SkinEngine.Core.Rendering;
using SkinEngine.Core.Skin;
using SkinEngine.Core;

/// <summary>
/// Equalizer window (275×116). Renders eq_main.bmp background with
/// draggable band sliders, power/auto buttons, and presets button.
/// </summary>
public sealed class EqWindow : Form
{
    private readonly D2DSharedDevice _sharedDevice;
    private D2DWindowSurface? _surface;
    private SkinAtlas? _atlas;
    private SkinRenderer? _renderer;

    private System.Threading.Timer? _frameTimer;
    private readonly object _renderLock = new();

    private const int BASE_W = 275;
    private const int BASE_H = 116;
    public int ScaleFactor { get; private set; } = 1;

    public bool IsVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public EqWindow(D2DSharedDevice sharedDevice, SkinAtlas atlas, SkinDefinition skinDef, Vortice.DirectWrite.IDWriteFactory dwFactory)
    {
        _sharedDevice = sharedDevice;
        AppLogger.Info("[EqWindow] Constructing...");

        Text            = "Winamp EQ";
        ClientSize      = new Size(275, 116);
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
            AppLogger.Info("[EqWindow] Load event — creating surface and renderer...");
            _surface  = _sharedDevice.CreateWindowSurface(Handle, ClientSize.Width, ClientSize.Height);
            _atlas    = atlas;
            _renderer = new SkinRenderer(_surface, _atlas, skinDef, dwFactory);

            for (int i = 0; i < 10; i++)
                _renderer.EqBands[i] = 0.5f;
            _renderer.EqPreamp = 0.5f;

            AppLogger.Info("[EqWindow] Starting render timer...");
            _frameTimer = new System.Threading.Timer(_ => RenderFrame(), null, 0, 16);
        };

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                AppLogger.Info("[EqWindow] Close requested — hiding instead");
                Hide();
                e.Cancel = true;
            }
        };

        AppLogger.Info("[EqWindow] Constructor done");
    }

    public void Toggle()
    {
        AppLogger.Info($"[EqWindow] Toggle — Visible was {Visible}, now {!Visible}");
        Visible = !Visible;
    }

    public void SetScale(int scale)
    {
        if (scale < 1) scale = 1;
        ScaleFactor = scale;
        SuspendLayout();
        ClientSize = new Size(BASE_W * scale, BASE_H * scale);
        ResumeLayout(false);
    }

    public void SetPlaylistConfig(PleditConfig? config)
    {
        lock (_renderLock)
        {
            if (_renderer is not null)
                _renderer.PlaylistConfig = config;
        }
    }

    public void SetVisColors(uint[]? visColors)
    {
        lock (_renderLock)
        {
            if (_renderer is not null)
                _renderer.VisColors = visColors;
        }
    }

    public void SetEqBands(float[] bands, float preamp)
    {
        lock (_renderLock)
        {
            if (_renderer is null) return;
            for (int i = 0; i < 10 && i < bands.Length; i++)
                _renderer.EqBands[i] = bands[i];
            _renderer.EqPreamp = preamp;
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
            int scale = (Owner as PlayerWindow)?.ScaleFactor ?? 1;
            Location = new Point(Owner.Left, Owner.Top + 116 * scale);
        }
        else
        {
            Location = new Point(100, 100 + 116);
        }
        AppLogger.Info($"[EqWindow] Showing at ({Location.X}, {Location.Y})");
        base.Show(Owner);
    }

    private void RenderFrame()
    {
        lock (_renderLock)
        {
            if (_surface is null || _renderer is null) return;

            _surface.BeginDraw();
            if (ScaleFactor > 1)
                _surface.DeviceContext.Transform = System.Numerics.Matrix3x2.CreateScale((float)ScaleFactor, (float)ScaleFactor);
            _renderer.DrawEqWindow();
            if (ScaleFactor > 1)
                _surface.DeviceContext.Transform = System.Numerics.Matrix3x2.Identity;
            _surface.EndDraw();
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
