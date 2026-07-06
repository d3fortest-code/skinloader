namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class LabelDemo : DemoBase
{
    private Label? _label;
    private static readonly IDWriteFactory DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);
        _label = new Label(DwFactory, "lbl", "Hello, SkinEngine!", 50, 50);
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_label is not null && Atlas is not null)
            _label.Draw(dc, Atlas);
    }
}
