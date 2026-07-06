namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class IndicatorDemo : DemoBase
{
    private Indicator? _indicator;

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);
        _indicator = new Indicator("ind", 50, 50);
        _indicator.IsOn = true;
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_indicator is not null && Atlas is not null)
            _indicator.Draw(dc, Atlas);
    }
}
