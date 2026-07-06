namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class ToggleButtonDemo : DemoBase
{
    private ToggleButton? _toggle;
    private static readonly IDWriteFactory DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);
        _toggle = new ToggleButton("toggle", 50, 50, 23, 18);
        _toggle.Label = "EQ On/Off";
        _toggle.DwFactory = DwFactory;
        _toggle.OnToggle += t => SkinEngine.Core.AppLogger.Info($"[ToggleButtonDemo] State: {t.IsChecked}");
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_toggle is not null && Atlas is not null)
            _toggle.Draw(dc, Atlas);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        float dpi = DeviceDpi;
        _toggle?.OnMouseDown((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        float dpi = DeviceDpi;
        _toggle?.OnMouseUp((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        Invalidate();
    }
}
