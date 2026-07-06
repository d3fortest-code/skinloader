namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;

/// <summary>
/// A button that toggles between two states (on/off).
/// Shuffle, Repeat, EQ On/Off, Mono/Stereo indicators are all ToggleButtons
/// when they accept user interaction.
/// </summary>
public class ToggleButton : Control
{
    private readonly Dictionary<(bool isChecked, PushButton.ButtonState state), (string? Source, System.Drawing.Rectangle SrcRect)> _states = new();
    private bool _isChecked;
    private PushButton.ButtonState _interactionState = PushButton.ButtonState.Normal;
    private IDWriteTextFormat? _textFormat;

    public string Label { get; set; } = "";
    public IDWriteFactory? DwFactory { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set { _isChecked = value; }
    }

    public event Action<ToggleButton>? OnToggle;

    public ToggleButton() { }

    public ToggleButton(string id, int x, int y, int w, int h)
    {
        Id = id;
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public void SetState(bool isChecked, PushButton.ButtonState state, string? source, System.Drawing.Rectangle srcRect)
    {
        _states[(isChecked, state)] = (source, srcRect);
    }

    public void SetState(bool isChecked, PushButton.ButtonState state, string? source, int srcX, int srcY, int srcW, int srcH)
    {
        _states[(isChecked, state)] = (source, new System.Drawing.Rectangle(srcX, srcY, srcW, srcH));
    }

    public override void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas)
    {
        if (!Visible) return;

        if (_states.TryGetValue((_isChecked, _interactionState), out var entry)
            && entry.Source is not null
            && atlas.TryGet(entry.Source, out var bmp))
        {
            var src = entry.SrcRect;
            var dst = new Rect(X, Y, X + Width, Y + Height);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            ctx.DrawBitmap(bmp, dst, 1f, BitmapInterpolationMode.NearestNeighbor, srcF);
        }
        else if (_states.Count == 0)
        {
            DrawFallback(ctx);
        }
    }

    private void DrawFallback(ID2D1DeviceContext5 ctx)
    {
        var bgColor = _isChecked ? _interactionState switch
        {
            PushButton.ButtonState.Normal => new Color4(0.12f, 0.35f, 0.15f, 1f),
            PushButton.ButtonState.Hover => new Color4(0.15f, 0.42f, 0.18f, 1f),
            PushButton.ButtonState.Pressed => new Color4(0.08f, 0.25f, 0.10f, 1f),
            _ => new Color4(0.10f, 0.30f, 0.12f, 1f),
        } : _interactionState switch
        {
            PushButton.ButtonState.Normal => new Color4(0.22f, 0.24f, 0.30f, 1f),
            PushButton.ButtonState.Hover => new Color4(0.30f, 0.34f, 0.42f, 1f),
            PushButton.ButtonState.Pressed => new Color4(0.15f, 0.16f, 0.22f, 1f),
            _ => new Color4(0.15f, 0.15f, 0.18f, 1f),
        };

        var borderColor = _isChecked
            ? new Color4(0.30f, 0.65f, 0.35f, 1f)
            : new Color4(0.40f, 0.45f, 0.55f, 1f);

        using var bgBrush = ctx.CreateSolidColorBrush(bgColor);
        ctx.FillRectangle(new Rect(X, Y, X + Width, Y + Height), bgBrush);

        using var borderBrush = ctx.CreateSolidColorBrush(borderColor);
        ctx.DrawRectangle(new Rect(X + 0.5f, Y + 0.5f, X + Width - 0.5f, Y + Height - 0.5f), borderBrush, 1f);

        if (!string.IsNullOrEmpty(Label) && DwFactory is not null)
        {
            _textFormat ??= DwFactory.CreateTextFormat("Segoe UI", null,
                FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 10f, "en-us");
            _textFormat.TextAlignment = TextAlignment.Center;
            _textFormat.ParagraphAlignment = ParagraphAlignment.Center;

            var textColor = _isChecked
                ? new Color4(0.6f, 1f, 0.65f, 1f)
                : new Color4(0.85f, 0.88f, 0.95f, 1f);

            using var textBrush = ctx.CreateSolidColorBrush(textColor);
            ctx.DrawText(Label, _textFormat, new Rect(X, Y, X + Width, Y + Height), textBrush);
        }
    }

    public override void OnMouseDown(int mx, int my) { if (Enabled) _interactionState = PushButton.ButtonState.Pressed; }
    public override void OnMouseUp(int mx, int my)
    {
        if (!Enabled || _interactionState != PushButton.ButtonState.Pressed) return;
        _interactionState = PushButton.ButtonState.Hover;
        if (HitTest(mx, my))
        {
            _isChecked = !_isChecked;
            OnToggle?.Invoke(this);
        }
    }
    public override void OnMouseMove(int mx, int my) { if (Enabled) _interactionState = HitTest(mx, my) ? PushButton.ButtonState.Hover : PushButton.ButtonState.Normal; }
    public override void OnMouseEnter() { if (Enabled) _interactionState = PushButton.ButtonState.Hover; }
    public override void OnMouseLeave() { if (Enabled) _interactionState = PushButton.ButtonState.Normal; }
}
