namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class SliderDemo : DemoBase
{
    private Slider? _slider;
    private Label? _valueLabel;
    private static readonly IDWriteFactory DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);

        _slider = new Slider("slider", 50, 50, isVertical: true);
        _slider.OnValueChanged += s => SkinEngine.Core.AppLogger.Info($"[SliderDemo] Value: {s.Value:F2}");

        _valueLabel = new Label(DwFactory, "lbl", "Value: 0.00", 150, 50);
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_slider is not null && Atlas is not null)
            _slider.Draw(dc, Atlas);
        if (_valueLabel is not null && Atlas is not null)
            _valueLabel.Draw(dc, Atlas);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        float dpi = DeviceDpi;
        _slider?.OnMouseDown((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        if (_slider is not null && _valueLabel is not null)
            _valueLabel.Text = $"Value: {_slider.Value:F2}";
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if ((MouseButtons & MouseButtons.Left) != 0)
        {
            float dpi = DeviceDpi;
            _slider?.OnMouseMove((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
            if (_slider is not null && _valueLabel is not null)
                _valueLabel.Text = $"Value: {_slider.Value:F2}";
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        float dpi = DeviceDpi;
        _slider?.OnMouseUp((int)(e.X * dpi / 96f), (int)(e.Y * dpi / 96f));
        Invalidate();
    }
}
