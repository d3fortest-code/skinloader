namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;

/// <summary>
/// A progress bar control. Displays a fill proportion based on Value.
/// Seek Progress and Loading are ProgressBars.
/// </summary>
public class ProgressBar : Control
{
    private float _value;

    public float Minimum { get; set; }
    public float Maximum { get; set; } = 1f;
    public bool IsVertical { get; set; }

    public string? TrackSource { get; set; }
    public System.Drawing.Rectangle TrackSrcRect { get; set; }
    public string? FillSource { get; set; }
    public System.Drawing.Rectangle FillSrcRect { get; set; }

    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, Minimum, Maximum);
    }

    public ProgressBar() { }

    public ProgressBar(string id, int x, int y, int w, int h)
    {
        Id = id;
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public override void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas)
    {
        if (!Visible) return;

        float ratio = (_value - Minimum) / (Maximum - Minimum);

        if (TrackSource is not null && atlas.TryGet(TrackSource, out var trackBmp))
        {
            var src = TrackSrcRect;
            var dst = new Rect(X, Y, X + Width, Y + Height);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(trackBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else
        {
            // Recessed dark track
            using var trackBg = ctx.CreateSolidColorBrush(new Color4(0.08f, 0.09f, 0.12f, 1f));
            ctx.FillRectangle(new Rect(X, Y, X + Width, Y + Height), trackBg);

            using var trackBorder = ctx.CreateSolidColorBrush(new Color4(0.16f, 0.18f, 0.22f, 1f));
            ctx.DrawRectangle(new Rect(X + 0.5f, Y + 0.5f, X + Width - 0.5f, Y + Height - 0.5f), trackBorder, 1f);
        }

        if (FillSource is not null && atlas.TryGet(FillSource, out var fillBmp))
        {
            var src = FillSrcRect;
            if (IsVertical)
            {
                int fillH = (int)(Height * ratio);
                int fillY = Y + Height - fillH;
                var dst = new Rect(X, fillY, X + Width, fillY + fillH);
                var srcF = new Rect(src.X, src.Y + src.Height - fillH, src.X + src.Width, src.Y + src.Height);
                ctx.DrawBitmap(fillBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
            }
            else
            {
                int fillW = (int)(Width * ratio);
                var dst = new Rect(X, Y, X + fillW, Y + Height);
                var srcF = new Rect(src.X, src.Y, src.X + fillW, src.Y + src.Height);
                ctx.DrawBitmap(fillBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
            }
        }
        else
        {
            // Gradient-like fill: bright at the leading edge
            var fillColor = new Color4(0.15f, 0.45f, 0.75f, 1f);
            var edgeColor = new Color4(0.25f, 0.60f, 0.90f, 1f);
            using var fillBrush = ctx.CreateSolidColorBrush(fillColor);
            using var edgeBrush = ctx.CreateSolidColorBrush(edgeColor);

            if (IsVertical)
            {
                int fillH = (int)(Height * ratio);
                if (fillH > 0)
                {
                    int fillY = Y + Height - fillH;
                    ctx.FillRectangle(new Rect(X + 1, fillY, X + Width - 1, Y + Height - 1), fillBrush);
                    // Bright edge at top
                    ctx.FillRectangle(new Rect(X + 1, fillY, X + Width - 1, fillY + 3), edgeBrush);
                }
            }
            else
            {
                int fillW = (int)(Width * ratio);
                if (fillW > 0)
                {
                    ctx.FillRectangle(new Rect(X + 1, Y + 1, X + fillW, Y + Height - 1), fillBrush);
                    // Bright edge at right
                    ctx.FillRectangle(new Rect(X + fillW - 3, Y + 1, X + fillW, Y + Height - 1), edgeBrush);
                }
            }
        }
    }
}
