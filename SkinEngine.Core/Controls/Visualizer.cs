namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using Vortice.Mathematics;
using SkinEngine.Core.Rendering;
using System.Numerics;

/// <summary>
/// An animated visualizer control. Supports spectrum analyzer, oscilloscope, and VU meter modes.
/// </summary>
public class Visualizer : Control
{
    public enum VisualizerType { Spectrum, Oscilloscope, VUMeter }

    public VisualizerType Type { get; set; } = VisualizerType.Spectrum;
    public int BandCount { get; set; } = 20;
    public float[] BandValues { get; set; } = [];
    public uint[]? Colors { get; set; }

    public Visualizer() { }

    public Visualizer(string id, int x, int y, int w, int h)
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

        switch (Type)
        {
            case VisualizerType.Spectrum:
                DrawSpectrum(ctx);
                break;
            case VisualizerType.Oscilloscope:
                DrawOscilloscope(ctx);
                break;
            case VisualizerType.VUMeter:
                DrawVUMeter(ctx);
                break;
        }
    }

    private void DrawSpectrum(ID2D1DeviceContext5 ctx)
    {
        if (BandValues.Length == 0) return;

        int barCount = Math.Min(BandValues.Length, BandCount);
        int barWidth = Width / barCount;
        int gap = Math.Max(1, barWidth / 4);
        barWidth -= gap;

        for (int i = 0; i < barCount; i++)
        {
            float val = BandValues[i];
            int barH = (int)(Height * val);
            if (barH <= 0) continue;

            int barX = X + i * (barWidth + gap);
            int barY = Y + Height - barH;

            var color = GetBarColor(i, val);
            using var brush = ctx.CreateSolidColorBrush(color);
            ctx.FillRectangle(new Rect(barX, barY, barX + barWidth, Y + Height), brush);
        }
    }

    private void DrawOscilloscope(ID2D1DeviceContext5 ctx)
    {
        if (BandValues.Length == 0) return;

        using var brush = ctx.CreateSolidColorBrush(new Color4(0.2f, 0.8f, 0.2f, 1f));
        
        int points = Math.Min(BandValues.Length, Width);
        float stepX = (float)Width / points;

        for (int i = 0; i < points - 1; i++)
        {
            float x1 = X + i * stepX;
            float y1 = Y + Height / 2f + BandValues[i] * Height / 2f;
            float x2 = X + (i + 1) * stepX;
            float y2 = Y + Height / 2f + BandValues[Math.Min(i + 1, points - 1)] * Height / 2f;

            ctx.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), brush, 1.5f);
        }
    }

    private void DrawVUMeter(ID2D1DeviceContext5 ctx)
    {
        if (BandValues.Length == 0) return;

        float level = BandValues[0];
        int segments = 20;
        int segHeight = Height / segments;
        int litSegments = (int)(segments * level);

        for (int i = 0; i < segments; i++)
        {
            int segY = Y + Height - (i + 1) * segHeight;
            bool lit = i < litSegments;

            Color4 color;
            if (lit)
            {
                float ratio = (float)i / segments;
                color = ratio < 0.6f
                    ? new Color4(0.2f, 0.7f, 0.2f, 1f)
                    : ratio < 0.8f
                        ? new Color4(0.8f, 0.8f, 0.1f, 1f)
                        : new Color4(0.9f, 0.1f, 0.1f, 1f);
            }
            else
            {
                color = new Color4(0.15f, 0.15f, 0.2f, 1f);
            }

            using var brush = ctx.CreateSolidColorBrush(color);
            ctx.FillRectangle(new Rect(X, segY, X + Width, segY + segHeight - 1), brush);
        }
    }

    private Color4 GetBarColor(int bandIndex, float value)
    {
        if (Colors is { Length: > 0 })
        {
            int idx = Math.Clamp((int)(value * (Colors.Length - 1)), 0, Colors.Length - 1);
            uint c = Colors[idx];
            return new Color4(
                ((c >> 16) & 0xFF) / 255f,
                ((c >> 8) & 0xFF) / 255f,
                (c & 0xFF) / 255f, 1f);
        }

        float r = 0.1f + value * 0.1f;
        float g = 0.2f + value * 0.3f;
        float b = 0.6f + value * 0.4f;
        return new Color4(r, g, b, 1f);
    }
}
