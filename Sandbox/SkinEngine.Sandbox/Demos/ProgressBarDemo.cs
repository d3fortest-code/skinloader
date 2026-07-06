namespace SkinEngine.Sandbox.Demos;

using System.Drawing;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using SkinEngine.Core.Controls;
using SkinEngine.Core.Rendering;

public class ProgressBarDemo : DemoBase
{
    private ProgressBar? _progressBar;
    private Label? _label;
    private float _value;
    private static readonly IDWriteFactory DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);

        _progressBar = new ProgressBar("pb", 50, 50, 200, 15);
        _progressBar.Value = 0.5f;

        _label = new Label(DwFactory, "lbl", "Progress: 50%", 50, 80);

        var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (s, e) =>
        {
            _value += 0.01f;
            if (_value > 1f) _value = 0f;
            if (_progressBar is not null) _progressBar.Value = _value;
            if (_label is not null) _label.Text = $"Progress: {(int)(_value * 100)}%";
            Invalidate();
        };
        timer.Start();
    }

    protected override void Draw(ID2D1DeviceContext5 dc)
    {
        using var brush = dc.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.15f, 0.15f, 0.2f, 1f));
        dc.FillRectangle(new Vortice.Mathematics.Rect(0, 0, ClientSize.Width * DeviceDpi / 96f, ClientSize.Height * DeviceDpi / 96f), brush);

        if (_progressBar is not null && Atlas is not null)
            _progressBar.Draw(dc, Atlas);
        if (_label is not null && Atlas is not null)
            _label.Draw(dc, Atlas);
    }
}
