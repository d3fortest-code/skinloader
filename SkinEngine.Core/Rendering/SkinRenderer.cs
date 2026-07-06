namespace SkinEngine.Core.Rendering;

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using SkinEngine.Core.Skin;
using System.Drawing;

/// <summary>
/// Renders a complete Winamp skin window (main, equalizer, or playlist)
/// using Direct2D. Manages sprite drawing, slider positioning, digit readouts,
/// and text rendering via DirectWrite.
/// </summary>
public sealed class SkinRenderer
{
    private readonly D2DWindowSurface _surface;
    private readonly SkinAtlas        _atlas;
    private SkinDefinition            _def;

    private readonly IDWriteFactory    _dwFactory;
    private readonly IDWriteTextFormat _trackFormat;

    public double  PlaybackPosition { get; set; }
    public int     ElapsedSeconds   { get; set; }
    public bool    IsPlaying        { get; set; }
    public bool    IsPaused         { get; set; }
    public bool    IsStereo         { get; set; }
    public bool    TitleBarActive   { get; set; } = true;
    public bool    FreezeTimeDisplay { get; set; }
    public bool    FreezeSeekBar   { get; set; }

    public int    VolumeLevel  { get; set; } = 27;
    public int    BalanceLevel { get; set; } = 14;

    public bool   EqEnabled   { get; set; } = true;
    public bool   EqAuto      { get; set; }
    public float[] EqBands    { get; } = new float[10];
    public float   EqPreamp   { get; set; } = 0.5f;

    public List<string> Tracks       { get; set; } = new();
    public int          ScrollOffset { get; set; }
    public int          SelectedTrack { get; set; } = -1;
    public PleditConfig? PlaylistConfig { get; set; }
    public uint[]?       VisColors    { get; set; }

    public SkinRenderer(D2DWindowSurface surface, SkinAtlas atlas, SkinDefinition def, IDWriteFactory? dwFactory = null)
    {
        _surface = surface;
        _atlas   = atlas;
        _def     = def;

        _dwFactory = dwFactory ?? DWrite.DWriteCreateFactory<IDWriteFactory>();
        _trackFormat = _dwFactory.CreateTextFormat("Tahoma", null,
            FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal,
            11f, "en-us");
    }

    public void SetDefinition(SkinDefinition def) => _def = def;

    public void DrawMainWindow()
    {
        if (!_def.Windows.TryGetValue("main", out var win)) return;

        foreach (var layer in win.Layers)
        {
            switch (layer.Type.ToLowerInvariant())
            {
                case "sprite":        DrawSprite(layer);        break;
                case "button":        DrawSprite(layer);        break;
                case "slider":        DrawSlider(layer);        break;
                case "digit_readout": DrawDigitReadout(layer);  break;
            }
        }
    }

    public void DrawEqWindow()
    {
        var dc = _surface.DeviceContext;
        int winW = 275, winH = 116;

        int eqW = 275, eqH = 116;
        _atlas.TryGetSize("eqmain.bmp", out eqW, out eqH);

        if (_atlas.TryGet("eqmain.bmp", out var eqBg))
        {
            var dst = new Rect(0, 0, winW, winH);
            var src = new Rect(0, 0, Math.Min(eqW, winW), Math.Min(eqH, winH));
            dc.DrawBitmap(eqBg, dst, 1f, BitmapInterpolationMode.NearestNeighbor, src);
        }
        else
        {
            using var brush = dc.CreateSolidColorBrush(new Color4(0.2f, 0.2f, 0.25f, 1f));
            dc.FillRectangle(new Rect(0, 0, winW, winH), brush);
        }

        if (_atlas.TryGet("titlebar.bmp", out var tb)
            && _atlas.TryGetSize("titlebar.bmp", out int tbW, out int tbH))
        {
            int barH = Math.Min(14, tbH / 4);
            var src = TitleBarActive
                ? new Rect(0, 0, tbW, barH)
                : new Rect(0, barH + 1, tbW, barH);
            var dst = new Rect(0, 0, winW, barH);
            dc.DrawBitmap(tb, dst, 1f, BitmapInterpolationMode.NearestNeighbor, src);
        }

        if (_atlas.TryGet("eqmain.bmp", out var eqMain))
        {
            void DrawEqClamped(int dstX, int dstY, int dstW, int dstH, int srcX, int srcY, int srcW, int srcH)
            {
                if (srcX + srcW > eqW) srcW = Math.Max(0, eqW - srcX);
                if (srcY + srcH > eqH) srcH = Math.Max(0, eqH - srcY);
                if (srcW <= 0 || srcH <= 0) return;
                dc.DrawBitmap(eqMain,
                    new Rect(dstX, dstY, dstX + dstW, dstY + dstH), 1f,
                    BitmapInterpolationMode.NearestNeighbor,
                    new Rect(srcX, srcY, srcX + srcW, srcY + srcH));
            }

            float scaleX = eqW / 275f;
            float scaleY = eqH / 116f;
            int SX(int x) => (int)(x * scaleX);
            int SY(int y) => (int)(y * scaleY);

            int powerSrcY = EqEnabled ? SY(15) : SY(3);
            DrawEqClamped(SX(10), SY(3), SX(11), SY(12), SX(10), powerSrcY, SX(11), SY(12));

            int autoSrcY = EqAuto ? SY(15) : SY(3);
            DrawEqClamped(SX(25), SY(3), SX(33), SY(12), SX(25), autoSrcY, SX(33), SY(12));

            DrawEqClamped(SX(217), SY(0), SX(44), SY(12), SX(217), SY(0), SX(44), SY(12));

            int[] bandXSrc = [16, 32, 48, 64, 80, 96, 112, 128, 144, 160];

            for (int i = 0; i < 10; i++)
            {
                float val = EqBands[i];
                int thumbY = SY(17) + (int)((1f - val) * (SY(85) - SY(17)));
                DrawEqClamped(SX(bandXSrc[i]), thumbY, SX(11), SY(11), SX(13), SY(164), SX(11), SY(11));
            }

            int preampY = SY(17) + (int)((1f - EqPreamp) * (SY(85) - SY(17)));
            DrawEqClamped(SX(0), preampY, SX(11), SY(11), SX(13), SY(164), SX(11), SY(11));

            int barWidth = SX(14);
            int barTop = SY(87);
            int barBottom = SY(100);
            int barMaxH = barBottom - barTop;

            for (int i = 0; i < 10; i++)
            {
                float val = EqBands[i];
                int barH = (int)(val * barMaxH);
                if (barH <= 0) continue;

                int barX = SX(bandXSrc[i]) - 1;
                int barY = barBottom - barH;

                var barColor = GetEqBarColor(i, val);
                using var brush = dc.CreateSolidColorBrush(barColor);
                dc.FillRectangle(new Rect(barX, barY, barX + barWidth, barBottom), brush);
            }
        }
    }

    public void DrawPlaylistWindow(int windowWidth, int windowHeight)
    {
        var dc = _surface.DeviceContext;

        if (_atlas.TryGet("pledit.bmp", out var plBmp)
            && _atlas.TryGetSize("pledit.bmp", out int plW, out int plH))
        {
            int topChromeH = Math.Min(20, plH / 4);
            int bottomChromeH = Math.Min(38, plH / 4);
            int btnRowY = Math.Min(111, plH - 18);
            int cornerW = Math.Min(25, plW / 10);
            int cornerH = Math.Min(20, plH / 5);
            int rightEdgeX = Math.Max(cornerW + 1, plW - 21);
            int leftEdgeW = Math.Min(12, plW / 20);
            int rightEdgeW = Math.Min(14, plW / 20);
            int bottomLeftW = Math.Min(125, plW / 2);
            int bottomRightX = Math.Min(bottomLeftW, plW - 150);
            int bottomRightW = Math.Min(150, plW - bottomRightX);

            int contentTop    = topChromeH;
            int contentBottom = windowHeight - bottomChromeH;

            void DrawClamped(int dstX, int dstY, int dstW, int dstH, int srcX, int srcY, int srcW, int srcH)
            {
                if (srcX + srcW > plW) srcW = Math.Max(0, plW - srcX);
                if (srcY + srcH > plH) srcH = Math.Max(0, plH - srcY);
                if (srcW <= 0 || srcH <= 0) return;

                dc.DrawBitmap(plBmp,
                    new Rect(dstX, dstY, dstX + dstW, dstY + dstH), 1f,
                    BitmapInterpolationMode.NearestNeighbor,
                    new Rect(srcX, srcY, srcX + srcW, srcY + srcH));
            }

            DrawClamped(0, 0, cornerW, topChromeH, 0, 0, cornerW, topChromeH);
            DrawClamped(cornerW, 0, windowWidth - cornerW - (plW - rightEdgeX), topChromeH, cornerW, 0, 1, topChromeH);
            DrawClamped(windowWidth - (plW - rightEdgeX), 0, plW - rightEdgeX, topChromeH, rightEdgeX, 0, plW - rightEdgeX, topChromeH);

            int edgeH = contentBottom - contentTop;
            if (edgeH > 0)
            {
                DrawClamped(0, contentTop, leftEdgeW, edgeH, 0, topChromeH, leftEdgeW, 1);
                DrawClamped(windowWidth - rightEdgeW, contentTop, rightEdgeW, edgeH, plW - rightEdgeW, topChromeH, rightEdgeW, 1);
                DrawClamped(windowWidth - rightEdgeW, contentTop, rightEdgeW, edgeH, plW - rightEdgeW, topChromeH, rightEdgeW, 1);
            }

            int sbX = windowWidth - rightEdgeW;

            int bottomH = windowHeight - contentBottom;
            DrawClamped(0, contentBottom, bottomLeftW, bottomH, 0, btnRowY - 38, bottomLeftW, Math.Min(bottomChromeH, plH - (btnRowY - 38)));
            DrawClamped(windowWidth - bottomRightW, contentBottom, bottomRightW, bottomH, bottomRightX, btnRowY - 38, bottomRightW, Math.Min(bottomChromeH, plH - (btnRowY - 38)));

            int btnW = Math.Min(21, plW / 5);
            int btnGap = Math.Min(28, btnW + 7);
            int btnH = Math.Min(9, bottomChromeH / 4);
            for (int b = 0; b < 4; b++)
            {
                int btnX = b * btnGap;
                int srcBtnX = b * btnGap;
                if (srcBtnX + btnW <= plW && btnRowY + btnH <= plH)
                    DrawClamped(btnX, windowHeight - btnH - 1, btnW, btnH, srcBtnX, btnRowY, btnW, btnH);
            }

            int totalTracks = Math.Max(1, Tracks.Count);
            int visibleTracks = Math.Max(1, edgeH / 14);
            int thumbHeight = Math.Max(18, Math.Min(edgeH, (int)(52f * visibleTracks / totalTracks)));
            int trackRange = Math.Max(1, 52 - thumbHeight);
            float scrollFrac = totalTracks <= visibleTracks ? 0f : (float)ScrollOffset / (totalTracks - visibleTracks);
            int thumbY = contentTop + (int)(trackRange * scrollFrac);

            int thumbSrcX = Math.Min(plW - 8, plW - 8);
            int thumbSrcY = 0;
            if (thumbSrcX < plW && thumbSrcY + 18 <= plH)
                DrawClamped(sbX, thumbY, rightEdgeW, thumbHeight, thumbSrcX, thumbSrcY, 8, 18);
        }
        else
        {
            using var brush = dc.CreateSolidColorBrush(new Color4(0.1f, 0.1f, 0.15f, 1f));
            dc.FillRectangle(new Rect(0, 0, windowWidth, windowHeight), brush);
        }

        DrawTrackList(dc, windowWidth, windowHeight);
    }

    private void DrawClamped(ID2D1DeviceContext5 dc, ID2D1Bitmap1 bmp, int plW, int plH,
        int dstX, int dstY, int dstW, int dstH, int srcX, int srcY, int srcW, int srcH)
    {
        if (srcX + srcW > plW) srcW = Math.Max(0, plW - srcX);
        if (srcY + srcH > plH) srcH = Math.Max(0, plH - srcY);
        if (srcW <= 0 || srcH <= 0) return;

        dc.DrawBitmap(bmp,
            new Rect(dstX, dstY, dstX + dstW, dstY + dstH), 1f,
            BitmapInterpolationMode.NearestNeighbor,
            new Rect(srcX, srcY, srcX + srcW, srcY + srcH));
    }

    private void DrawPlButton(ID2D1DeviceContext5 dc, ID2D1Bitmap1 plBmp, int plW, int plH,
        int x, int y, string button, bool hover)
    {
        int srcY = hover ? 120 : 111;
        int srcX = button switch
        {
            "add"    => 0,
            "remove" => 28,
            "select" => 56,
            "misc"   => 84,
            _        => 0
        };
        DrawClamped(dc, plBmp, plW, plH, x, y, 21, 9, srcX, srcY, 21, 9);
    }

    private void DrawTrackList(ID2D1DeviceContext5 dc, int windowWidth, int windowHeight)
    {
        if (Tracks.Count == 0) return;

        int contentTop    = 20;
        int contentBottom = windowHeight - 38;
        int contentLeft   = 14;
        int contentWidth  = windowWidth - 28;
        int lineHeight    = 14;

        var cfg = PlaylistConfig;
        var normalColor = cfg is not null
            ? new Color4(((cfg.TextColor >> 16) & 0xFF) / 255f,
                         ((cfg.TextColor >> 8) & 0xFF) / 255f,
                         (cfg.TextColor & 0xFF) / 255f, 1f)
            : new Color4(0f, 0f, 0.8f, 1f);

        var selectedTextColor = cfg is not null
            ? new Color4(((cfg.CurrentColor >> 16) & 0xFF) / 255f,
                         ((cfg.CurrentColor >> 8) & 0xFF) / 255f,
                         (cfg.CurrentColor & 0xFF) / 255f, 1f)
            : new Color4(1f, 1f, 1f, 1f);

        var selectedBgColor = cfg is not null
            ? new Color4(((cfg.SelectedBgColor >> 16) & 0xFF) / 255f,
                         ((cfg.SelectedBgColor >> 8) & 0xFF) / 255f,
                         (cfg.SelectedBgColor & 0xFF) / 255f, 1f)
            : new Color4(0f, 0f, 0.8f, 1f);

        using var normalBrush = dc.CreateSolidColorBrush(normalColor);
        using var selectedBrush = dc.CreateSolidColorBrush(selectedTextColor);
        using var selectedBgBrush = dc.CreateSolidColorBrush(selectedBgColor);

        dc.PushAxisAlignedClip(
            new Rect(contentLeft, contentTop, contentLeft + contentWidth, contentBottom),
            AntialiasMode.Aliased);

        int maxVisible = (contentBottom - contentTop) / lineHeight;
        int firstVisible = ScrollOffset;
        int lastVisible = Math.Min(firstVisible + maxVisible, Tracks.Count);

        for (int i = firstVisible; i < lastVisible; i++)
        {
            int y = contentTop + (i - firstVisible) * lineHeight;

            if (i == SelectedTrack)
            {
                dc.DrawRectangle(
                    new Rect(contentLeft, y, contentLeft + contentWidth, y + lineHeight),
                    selectedBgBrush, 1f);
            }

            var brush = i == SelectedTrack ? selectedBrush : normalBrush;
            var rect = new Rect(contentLeft + 2, y, contentLeft + contentWidth - 2, y + lineHeight);
            dc.DrawText(Tracks[i], _trackFormat, rect, brush);
        }

        dc.PopAxisAlignedClip();
    }

    private void DrawSprite(LayerDef layer)
    {
        if (layer.Source is null || layer.SrcRect is null) return;
        if (!_atlas.TryGet(layer.Source, out var bmp)) return;

        var src = ResolveStateRect(layer);

        if (_atlas.TryGetSize(layer.Source, out int bmpW, out int bmpH))
        {
            if (src.X + src.Width > bmpW)  src.Width  = Math.Max(0, bmpW - src.X);
            if (src.Y + src.Height > bmpH) src.Height = Math.Max(0, bmpH - src.Y);
        }

        var dst = new Rect(
            layer.Dst[0], layer.Dst[1],
            layer.Dst[0] + src.Width,
            layer.Dst[1] + src.Height);

        var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);

        _surface.DeviceContext.DrawBitmap(bmp, dst, 1f,
            BitmapInterpolationMode.NearestNeighbor, srcF);
    }

    private void DrawSlider(LayerDef layer)
    {
        bool isVertical = layer.Bind == "volume" || layer.Bind == "balance";

        if (layer.Track?.Source is not null && layer.Track.SrcRect is not null
            && _atlas.TryGet(layer.Track.Source, out var trackBmp))
        {
            var s = layer.Track.SrcRect;

            int bmpW = 0, bmpH = 0;
            _atlas.TryGetSize(layer.Track.Source, out bmpW, out bmpH);
            int srcX = Math.Clamp(s.X, 0, Math.Max(0, bmpW - 1));
            int srcY = Math.Clamp(s.Y, 0, Math.Max(0, bmpH - 1));
            int srcW = Math.Clamp(s.W, 0, bmpW - srcX);
            int srcH = Math.Clamp(s.H, 0, bmpH - srcY);

            if (isVertical)
            {
                int level = layer.Bind == "volume" ? VolumeLevel : BalanceLevel;
                int sliceH = 15;
                int sliceY = srcY + level * sliceH;
                int drawW = Math.Min(srcW, 68);
                int drawH = Math.Min(sliceH, Math.Max(0, (srcY + srcH) - sliceY));

                if (drawW > 0 && drawH > 0)
                {
                    var dst = new Rect(layer.Dst[0], layer.Dst[1], layer.Dst[0] + drawW, layer.Dst[1] + sliceH);
                    var src = new Rect(srcX, sliceY, srcX + drawW, sliceY + drawH);
                    _surface.DeviceContext.DrawBitmap(trackBmp, dst, 1f,
                        BitmapInterpolationMode.NearestNeighbor, src);
                }
            }
            else
            {
                if (srcW > 0 && srcH > 0)
                {
                    var dst = new Rect(layer.Dst[0], layer.Dst[1],
                                       layer.Dst[0] + srcW, layer.Dst[1] + srcH);
                    var src = new Rect(srcX, srcY, srcX + srcW, srcY + srcH);
                    _surface.DeviceContext.DrawBitmap(trackBmp, dst, 1f,
                        BitmapInterpolationMode.NearestNeighbor, src);
                }
            }
        }

        if (layer.Thumb?.Source is not null && layer.Thumb.SrcRect is not null
            && _atlas.TryGet(layer.Thumb.Source, out var thumbBmp))
        {
            var s = layer.Thumb.SrcRect;

            int tBmpW = 0, tBmpH = 0;
            _atlas.TryGetSize(layer.Thumb.Source, out tBmpW, out tBmpH);
            int tSrcX = Math.Clamp(s.X, 0, Math.Max(0, tBmpW - 1));
            int tSrcY = Math.Clamp(s.Y, 0, Math.Max(0, tBmpH - 1));
            int tSrcW = Math.Clamp(s.W, 0, tBmpW - tSrcX);
            int tSrcH = Math.Clamp(s.H, 0, tBmpH - tSrcY);

            if (tSrcW <= 0 || tSrcH <= 0) return;

            if (isVertical)
            {
                int level = layer.Bind == "volume" ? VolumeLevel : BalanceLevel;
                int sliceH = 15;
                int thumbH = tSrcH;

                int trackH = layer.Track?.SrcRect?.H ?? (28 * sliceH);
                int travelRange = Math.Max(0, trackH - thumbH);
                int thumbY = layer.Dst[1] + (travelRange * (27 - level) / 27);

                var dst = new Rect(layer.Dst[0], thumbY, layer.Dst[0] + tSrcW, thumbY + thumbH);
                var src = new Rect(tSrcX, tSrcY, tSrcX + tSrcW, tSrcY + tSrcH);
                _surface.DeviceContext.DrawBitmap(thumbBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
            else
            {
                double pos = layer.Bind == "playback.position" ? PlaybackPosition : 0.5;
                int trackW = layer.Track?.SrcRect?.W ?? tBmpW;
                int thumbW = tSrcW;
                int thumbX = layer.Dst[0] + (int)((trackW - thumbW) * pos);
                int thumbY = layer.Dst[1];

                var dst = new Rect(thumbX, thumbY, thumbX + tSrcW, thumbY + tSrcH);
                var src = new Rect(tSrcX, tSrcY, tSrcX + tSrcW, tSrcY + tSrcH);
                _surface.DeviceContext.DrawBitmap(thumbBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
        }
    }

    private void DrawDigitReadout(LayerDef layer)
    {
        if (layer.Source is null || !_atlas.TryGet(layer.Source, out var bmp)) return;

        int dw = layer.DigitSize?[0] ?? 9;
        int dh = layer.DigitSize?[1] ?? 13;

        int elapsed  = layer.Bind == "playback.elapsed" ? ElapsedSeconds : 0;
        bool negative = elapsed < 0;
        int  abs      = Math.Abs(elapsed);
        int  minutes  = abs / 60;
        int  seconds  = abs % 60;

        var digits = new List<int?>();
        if (negative) digits.Add(null);
        digits.Add(minutes / 10);
        digits.Add(minutes % 10);
        digits.Add(seconds / 10);
        digits.Add(seconds % 10);

        int x = layer.Dst[0];
        int y = layer.Dst[1];

        foreach (var d in digits)
        {
            System.Drawing.Rectangle src = d.HasValue
                ? SpriteRegions.Numbers.Digit(d.Value)
                : SpriteRegions.Numbers.Minus;

            var dst = new Rect(x, y, x + dw, y + dh);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            _surface.DeviceContext.DrawBitmap(bmp, dst, 1f,
                BitmapInterpolationMode.NearestNeighbor, srcF);
            x += dw;
        }
    }

    private Color4 GetEqBarColor(int bandIndex, float value)
    {
        if (VisColors is { Length: > 0 })
        {
            int colorIdx = Math.Clamp((int)(value * (VisColors.Length - 1)), 0, VisColors.Length - 1);
            uint c = VisColors[colorIdx];
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

    private System.Drawing.Rectangle ResolveStateRect(LayerDef layer)
    {
        if (!TitleBarActive
            && layer.Id == "titlebar"
            && layer.States?.TryGetValue("inactive", out var s) == true
            && s.SrcRect is not null)
        {
            return s.SrcRect.ToRectangle();
        }
        return layer.SrcRect?.ToRectangle() ?? System.Drawing.Rectangle.Empty;
    }
}