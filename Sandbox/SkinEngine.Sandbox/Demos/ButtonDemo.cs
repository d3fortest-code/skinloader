namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class ButtonDemo : DemoBase
{
    private PushButton? _button;
    private static readonly IDWriteFactory DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);
        _button = new PushButton("btn", 50, 50, 23, 18);
        _button.Label = "Play";
        _button.DwFactory = DwFactory;
        _button.OnClick += b => SkinEngine.Core.AppLogger.Info("[ButtonDemo] Button clicked!");
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_button is not null && Atlas is not null)
            _button.Draw(dc, Atlas);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        float dpi = DeviceDpi;
        _button?.OnMouseDown((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        float dpi = DeviceDpi;
        _button?.OnMouseUp((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        Invalidate();
    }
}
