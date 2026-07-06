namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;

/// <summary>
/// A text label control. Renders text via DirectWrite.
/// Song Title, Time, Bitrate, Sample Rate are all Labels.
/// </summary>
public class Label : Control
{
    private readonly IDWriteFactory _dwFactory;
    private IDWriteTextFormat? _textFormat;

    public string Text { get; set; } = "";
    public Color4 Color { get; set; } = new(1f, 1f, 1f, 1f);
    public string FontName { get; set; } = "Tahoma";
    public float FontSize { get; set; } = 11f;
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Leading;
    public ParagraphAlignment ParagraphAlignment { get; set; } = ParagraphAlignment.Near;

    public Label(IDWriteFactory dwFactory)
    {
        _dwFactory = dwFactory;
    }

    public Label(IDWriteFactory dwFactory, string id, string text, int x, int y)
    {
        _dwFactory = dwFactory;
        Id = id;
        Text = text;
        X = x;
        Y = y;
    }

    private void EnsureFormat()
    {
        if (_textFormat is not null) return;
        _textFormat = _dwFactory.CreateTextFormat(FontName, null,
            FontWeight, FontStyle.Normal, FontStretch.Normal,
            FontSize, "en-us");
        _textFormat.TextAlignment = TextAlignment;
        _textFormat.ParagraphAlignment = ParagraphAlignment;
    }

    public override void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        EnsureFormat();

        using var brush = ctx.CreateSolidColorBrush(Color);
        var rect = new Rect(X, Y, X + Width, Y + Height);
        ctx.DrawText(Text, _textFormat!, rect, brush);
    }
}
