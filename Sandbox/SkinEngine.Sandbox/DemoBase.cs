namespace SkinEngine.Sandbox;

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Numerics;
using SkinEngine.Core;
using SkinEngine.Core.Rendering;
using SkinEngine.Core.Skin;

public abstract class DemoBase : Form
{
    protected D2DSharedDevice SharedDevice { get; private set; } = null!;
    protected D2DWindowSurface Surface { get; private set; } = null!;
    protected SkinAtlas Atlas { get; private set; } = null!;
    protected WszSkin? Skin { get; set; }
    protected SkinDefinition? Definition { get; set; }

    private const int BASE_W = 800;
    private const int BASE_H = 600;

    protected DemoBase()
    {
        Text = GetType().Name;
        int scale = Program.ScaleFactor;
        ClientSize = new Size(BASE_W * scale, BASE_H * scale);
        StartPosition = FormStartPosition.CenterScreen;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        AppLogger.Info($"[DemoBase] Loading demo: {GetType().Name}");

        SharedDevice = D2DSharedDevice.Create();

        float dpi = DeviceDpi;
        int scale = Program.ScaleFactor;
        int logicalW = ClientSize.Width;
        int logicalH = ClientSize.Height;
        int physicalW = (int)(logicalW * dpi / 96f);
        int physicalH = (int)(logicalH * dpi / 96f);

        AppLogger.Info($"[DemoBase] DPI={dpi}, scale={scale}x, logical={logicalW}x{logicalH}, physical={physicalW}x{physicalH}");

        Surface = SharedDevice.CreateWindowSurface(Handle, physicalW, physicalH, dpi);
        Atlas = new SkinAtlas(Surface.DeviceContext);

        if (Program.SkinPath is not null)
        {
            AppLogger.Info($"[DemoBase] Loading skin: {Program.SkinPath}");
            Skin = WszSkinLoader.Load(Program.SkinPath);
            Atlas.UploadSkin(Skin);
            Definition = SkinDefinition.BuildClassicSilver();
            AppLogger.Info($"[DemoBase] Skin loaded — {Atlas.Count} bitmaps in atlas");
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Surface is not null && ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            float dpi = DeviceDpi;
            int physicalW = (int)(ClientSize.Width * dpi / 96f);
            int physicalH = (int)(ClientSize.Height * dpi / 96f);
            Surface.Resize(physicalW, physicalH);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Surface is null) return;
        int scale = Program.ScaleFactor;
        Surface.BeginDraw();
        if (scale > 1)
            Surface.DeviceContext.Transform = Matrix3x2.CreateScale(scale, scale);
        Draw(Surface.DeviceContext);
        if (scale > 1)
            Surface.DeviceContext.Transform = Matrix3x2.Identity;
        Surface.EndDraw();
    }

    protected abstract void Draw(Vortice.Direct2D1.ID2D1DeviceContext5 dc);

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        AppLogger.Info($"[DemoBase] Closing demo: {GetType().Name}");
        Atlas?.Dispose();
        Surface?.Dispose();
        SharedDevice?.Dispose();
        base.OnFormClosing(e);
    }
}
