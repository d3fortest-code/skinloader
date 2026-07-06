namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;

/// <summary>
/// A slider control with a track and thumb.
/// Volume, Balance, Seek, EQ Bands and Preamp are all Slider instances.
/// Supports both vertical and horizontal orientations.
/// </summary>
public class Slider : Control
{
    private float _value;
    private bool _isDragging;

    public float Minimum { get; set; }
    public float Maximum { get; set; } = 1f;
    public bool IsVertical { get; set; }
    public int ThumbWidth { get; set; }
    public int ThumbHeight { get; set; }

    public string? TrackSource { get; set; }
    public System.Drawing.Rectangle TrackSrcRect { get; set; }
    public string? ThumbSource { get; set; }
    public System.Drawing.Rectangle ThumbSrcRect { get; set; }

    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, Minimum, Maximum);
    }

    public event Action<Slider>? OnValueChanged;

    public Slider() { }

    public Slider(string id, int x, int y, bool isVertical)
    {
        Id = id;
        X = x;
        Y = y;
        IsVertical = isVertical;
    }

    public override void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas)
    {
        if (!Visible) return;

        if (TrackSource is not null && atlas.TryGet(TrackSource, out var trackBmp))
        {
            var src = TrackSrcRect;
            int drawW = Math.Min(src.Width, Width > 0 ? Width : src.Width);
            int drawH = Math.Min(src.Height, Height > 0 ? Height : src.Height);
            var dst = new Rect(X, Y, X + drawW, Y + drawH);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(trackBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else
        {
            DrawFallbackTrack(ctx);
        }

        if (ThumbSource is not null && atlas.TryGet(ThumbSource, out var thumbBmp))
        {
            var src = ThumbSrcRect;
            int thumbX, thumbY;

            if (IsVertical)
            {
                int travel = Height - src.Height;
                thumbX = X;
                thumbY = Y + (int)(travel * (1f - (_value - Minimum) / (Maximum - Minimum)));
            }
            else
            {
                int travel = Width - src.Width;
                thumbX = X + (int)(travel * (_value - Minimum) / (Maximum - Minimum));
                thumbY = Y;
            }

            var dst = new Rect(thumbX, thumbY, thumbX + src.Width, thumbY + src.Height);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(thumbBmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else
        {
            DrawFallbackThumb(ctx);
        }
    }

    private void DrawFallbackTrack(ID2D1DeviceContext5 ctx)
    {
        // Dark recessed track
        using var trackBg = ctx.CreateSolidColorBrush(new Color4(0.10f, 0.11f, 0.14f, 1f));
        ctx.FillRectangle(new Rect(X, Y, X + Width, Y + Height), trackBg);

        using var trackBorder = ctx.CreateSolidColorBrush(new Color4(0.18f, 0.20f, 0.25f, 1f));
        ctx.DrawRectangle(new Rect(X + 0.5f, Y + 0.5f, X + Width - 0.5f, Y + Height - 0.5f), trackBorder, 1f);

        // Fill portion up to current value
        float ratio = (_value - Minimum) / (Maximum - Minimum);
        var fillColor = new Color4(0.18f, 0.35f, 0.55f, 1f);
        using var fillBrush = ctx.CreateSolidColorBrush(fillColor);

        if (IsVertical)
        {
            int fillH = (int)(Height * ratio);
            if (fillH > 0)
                ctx.FillRectangle(new Rect(X + 1, Y + Height - fillH, X + Width - 1, Y + Height - 1), fillBrush);
        }
        else
        {
            int fillW = (int)(Width * ratio);
            if (fillW > 0)
                ctx.FillRectangle(new Rect(X + 1, Y + 1, X + fillW, Y + Height - 1), fillBrush);
        }
    }

    private void DrawFallbackThumb(ID2D1DeviceContext5 ctx)
    {
        int tw = ThumbWidth > 0 ? ThumbWidth : (IsVertical ? Width : 14);
        int th = ThumbHeight > 0 ? ThumbHeight : (IsVertical ? 14 : Height);
        int thumbX, thumbY;

        if (IsVertical)
        {
            int travel = Height - th;
            thumbX = X;
            thumbY = Y + (int)(travel * (1f - (_value - Minimum) / (Maximum - Minimum)));
        }
        else
        {
            int travel = Width - tw;
            thumbX = X + (int)(travel * (_value - Minimum) / (Maximum - Minimum));
            thumbY = Y;
        }

        var bgColor = _isDragging
            ? new Color4(0.45f, 0.55f, 0.70f, 1f)
            : new Color4(0.35f, 0.42f, 0.55f, 1f);

        using var bgBrush = ctx.CreateSolidColorBrush(bgColor);
        ctx.FillRectangle(new Rect(thumbX, thumbY, thumbX + tw, thumbY + th), bgBrush);

        using var borderBrush = ctx.CreateSolidColorBrush(new Color4(0.55f, 0.65f, 0.80f, 1f));
        ctx.DrawRectangle(new Rect(thumbX + 0.5f, thumbY + 0.5f, thumbX + tw - 0.5f, thumbY + th - 0.5f), borderBrush, 1f);
    }

    public override void OnMouseDown(int mx, int my)
    {
        if (!Enabled || !HitTest(mx, my)) return;
        _isDragging = true;
        UpdateValueFromMouse(mx, my);
    }

    public override void OnMouseUp(int mx, int my)
    {
        _isDragging = false;
    }

    public override void OnMouseMove(int mx, int my)
    {
        if (_isDragging && Enabled)
            UpdateValueFromMouse(mx, my);
    }

    private void UpdateValueFromMouse(int mx, int my)
    {
        float oldValue = _value;

        if (IsVertical)
        {
            float ratio = 1f - (float)(my - Y) / Height;
            _value = Minimum + (Maximum - Minimum) * Math.Clamp(ratio, 0f, 1f);
        }
        else
        {
            float ratio = (float)(mx - X) / Width;
            _value = Minimum + (Maximum - Minimum) * Math.Clamp(ratio, 0f, 1f);
        }

        if (_value != oldValue)
            OnValueChanged?.Invoke(this);
    }
}
