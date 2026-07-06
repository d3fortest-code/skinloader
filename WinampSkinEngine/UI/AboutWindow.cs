namespace WinampSkinEngine.UI;

using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;
using SkinEngine.Core;

/// <summary>
/// About window (275×116). Displays Winamp branding text rendered via DirectWrite.
/// Auto-closes after 5 seconds or on any click.
/// </summary>
public sealed class AboutWindow : Form
{
    private readonly D2DSharedDevice _sharedDevice;
    private D2DWindowSurface? _surface;

    private System.Threading.Timer? _frameTimer;
    private System.Threading.Timer? _autoCloseTimer;
    private readonly object _renderLock = new();

    private readonly IDWriteFactory _dwFactory;
    private readonly IDWriteTextFormat _titleFormat;
    private readonly IDWriteTextFormat _bodyFormat;

    public AboutWindow(D2DSharedDevice sharedDevice)
    {
        _sharedDevice = sharedDevice;
        AppLogger.Info("[AboutWindow] Constructing...");

        Text            = "About Winamp";
        ClientSize      = new System.Drawing.Size(275, 116);
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = System.Drawing.Color.Black;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint            |
                 ControlStyles.Opaque, true);
        UpdateStyles();

        _dwFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();

        _titleFormat = _dwFactory.CreateTextFormat("Arial", null,
            FontWeight.Bold, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal,
            16f, "en-us");
        _titleFormat.TextAlignment = TextAlignment.Center;
        _titleFormat.ParagraphAlignment = ParagraphAlignment.Center;

        _bodyFormat = _dwFactory.CreateTextFormat("Arial", null,
            FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal,
            10f, "en-us");
        _bodyFormat.TextAlignment = TextAlignment.Center;
        _bodyFormat.ParagraphAlignment = ParagraphAlignment.Center;

        Click += (_, _) =>
        {
            AppLogger.Info("[AboutWindow] Clicked — closing");
            Close();
        };

        Load += (_, _) =>
        {
            AppLogger.Info("[AboutWindow] Load event — creating surface...");
            _surface = _sharedDevice.CreateWindowSurface(Handle, ClientSize.Width, ClientSize.Height);

            AppLogger.Info("[AboutWindow] Starting render timer...");
            _frameTimer = new System.Threading.Timer(_ => RenderFrame(), null, 0, 16);
        };

        FormClosing += (_, _) =>
        {
            AppLogger.Info("[AboutWindow] FormClosing — disposing timers and surface");
            _frameTimer?.Dispose();
            _autoCloseTimer?.Dispose();
            _surface?.Dispose();
            _surface = null;
        };

        AppLogger.Info("[AboutWindow] Constructor done");
    }

    private void RenderFrame()
    {
        lock (_renderLock)
        {
            if (_surface is null) return;

            var dc = _surface.DeviceContext;

            _surface.BeginDraw();
            dc.Clear(new Color4(0.1f, 0.1f, 0.3f, 1f));

            using var titleBrush = dc.CreateSolidColorBrush(new Color4(1f, 0.85f, 0f, 1f));
            using var bodyBrush  = dc.CreateSolidColorBrush(new Color4(1f, 1f, 1f, 1f));

            dc.DrawText("WinampSkinEngine", _titleFormat,
                new Rect(0, 10, 275, 50), titleBrush);

            dc.DrawText("Classic Skin Renderer", _bodyFormat,
                new Rect(0, 50, 275, 70), bodyBrush);

            dc.DrawText("Based on Nullsoft Winamp", _bodyFormat,
                new Rect(0, 70, 275, 90), bodyBrush);

            dc.DrawText("Click to close", _bodyFormat,
                new Rect(0, 95, 275, 110), bodyBrush);

            _surface.EndDraw();
        }
    }

    protected override void OnPaint(PaintEventArgs e) { }
    protected override void OnPaintBackground(PaintEventArgs e) { }

    public void Show(Form owner)
    {
        Owner = owner;
        Show();
    }

    public new void Show()
    {
        if (Owner is not null)
        {
            Location = new Point(Owner.Left, Owner.Top + 116);
        }
        else
        {
            Location = new Point(100, 100);
        }
        AppLogger.Info($"[AboutWindow] Showing at ({Location.X}, {Location.Y})");

        // Restart the 5s auto-close timer each time the window is shown
        _autoCloseTimer?.Dispose();
        _autoCloseTimer = new System.Threading.Timer(_ =>
        {
            AppLogger.Info("[AboutWindow] Auto-close timer fired (5s)");
            if (IsHandleCreated) BeginInvoke(Close);
        }, null, 5000, System.Threading.Timeout.Infinite);

        base.Show(Owner);
    }
}
