namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;

/// <summary>
/// A visual indicator with on/off states. No user interaction.
/// Stereo, Mono, Shuffle and Repeat are all Indicators
/// when they don't accept user interaction.
/// </summary>
public class Indicator : Control
{
    public bool IsOn { get; set; }

    public string? OnSource { get; set; }
    public System.Drawing.Rectangle OnSrcRect { get; set; }
    public string? OffSource { get; set; }
    public System.Drawing.Rectangle OffSrcRect { get; set; }

    public Indicator() { }

    public Indicator(string id, int x, int y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public override void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas)
    {
        if (!Visible) return;

        if (IsOn && OnSource is not null && atlas.TryGet(OnSource, out var onBmp))
        {
            var src = OnSrcRect;
            var dst = new Rect(X, Y, X + src.Width, Y + src.Height);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(onBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else if (!IsOn && OffSource is not null && atlas.TryGet(OffSource, out var offBmp))
        {
            var src = OffSrcRect;
            var dst = new Rect(X, Y, X + src.Width, Y + src.Height);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(offBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else
        {
            // Fallback: colored rectangle
            var color = IsOn ? new Color4(0.2f, 0.8f, 0.2f, 1f) : new Color4(0.3f, 0.3f, 0.3f, 1f);
            using var brush = ctx.CreateSolidColorBrush(color);
            ctx.FillRectangle(new Rect(X, Y, X + Width, Y + Height), brush);
        }
    }
}
