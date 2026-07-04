namespace WinampSkinEngine.Rendering;

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using WinampSkinEngine.Skin;
using System.Drawing;

/// <summary>
/// Interprets a <see cref="SkinDefinition"/> layer tree and issues D2D draw calls
/// against a <see cref="SkinAtlas"/> each frame.
///
/// Owns no GPU resources itself — everything lives in <see cref="SkinAtlas"/>.
/// </summary>
public sealed class SkinRenderer
{
    private readonly D2DWindowSurface _surface;
    private readonly SkinAtlas        _atlas;
    private SkinDefinition            _def;

    // ── DirectWrite (created once, reused) ────────────────────────────────────
    private readonly IDWriteFactory    _dwFactory;
    private readonly IDWriteTextFormat _trackFormat;

    // ── Playback state (set by the caller each frame) ─────────────────────────
    public double  PlaybackPosition { get; set; }
    public int     ElapsedSeconds   { get; set; }
    public bool    IsPlaying        { get; set; }
    public bool    IsPaused         { get; set; }
    public bool    IsStereo         { get; set; }
    public bool    TitleBarActive   { get; set; } = true;

    // ── Volume/Balance state ──────────────────────────────────────────────────
    public int    VolumeLevel  { get; set; } = 27;  // 0-27 (28 positions)
    public int    BalanceLevel { get; set; } = 14;  // 0-27 (14 = center)

    // ── EQ state ──────────────────────────────────────────────────────────────
    public bool   EqEnabled   { get; set; } = true;
    public bool   EqAuto      { get; set; }
    public float[] EqBands    { get; } = new float[10];   // 0.0 – 1.0 per band
    public float   EqPreamp   { get; set; } = 0.5f;

    // ── Playlist state ────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draw one complete frame for the "main" window.
    /// Must be called between BeginDraw / EndDraw.
    /// </summary>
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

    // ── EQ Window ─────────────────────────────────────────────────────────────

    public void DrawEqWindow()
    {
        var dc = _surface.DeviceContext;
        int winW = 275, winH = 116;

        // Get actual EQ bitmap dimensions for adaptive layout
        int eqW = 275, eqH = 116;
        _atlas.TryGetSize("eqmain.bmp", out eqW, out eqH);

        // Background: eqmain.bmp full frame (if available)
        if (_atlas.TryGet("eqmain.bmp", out var eqBg))
        {
            var dst = new Rect(0, 0, winW, winH);
            var src = new Rect(0, 0, Math.Min(eqW, winW), Math.Min(eqH, winH));
            dc.DrawBitmap(eqBg, dst, 1f, BitmapInterpolationMode.NearestNeighbor, src);
        }
        else
        {
            // No EQ skin — draw a fallback background
            using var brush = dc.CreateSolidColorBrush(new Color4(0.2f, 0.2f, 0.25f, 1f));
            dc.FillRectangle(new Rect(0, 0, winW, winH), brush);
        }

        // Title bar (active state) — use actual titlebar.bmp dimensions
        if (_atlas.TryGet("titlebar.bmp", out var tb)
            && _atlas.TryGetSize("titlebar.bmp", out int tbW, out int tbH))
        {
            int barH = Math.Min(14, tbH / 4); // adaptive height
            var src = TitleBarActive
                ? new Rect(0, 0, tbW, barH)
                : new Rect(0, barH + 1, tbW, barH);
            var dst = new Rect(0, 0, winW, barH);
            dc.DrawBitmap(tb, dst, 1f, BitmapInterpolationMode.NearestNeighbor, src);
        }

        // Power/Auto/Presets buttons + band sliders (only if eqmain.bmp exists)
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

            // Scale EQ element positions to match actual bitmap size
            float scaleX = eqW / 275f;
            float scaleY = eqH / 116f;
            int SX(int x) => (int)(x * scaleX);
            int SY(int y) => (int)(y * scaleY);

            // Power button
            var powerSrc = EqEnabled ? new Rect(SX(10), SY(15), SX(11), SY(12)) : new Rect(SX(10), SY(3), SX(11), SY(12));
            DrawEqClamped(SX(10), SY(3), SX(11), SY(12), (int)powerSrc.X, (int)powerSrc.Y, (int)powerSrc.Width, (int)powerSrc.Height);

            // Auto button
            var autoSrc = EqAuto ? new Rect(SX(25), SY(15), SX(33), SY(12)) : new Rect(SX(25), SY(3), SX(33), SY(12));
            DrawEqClamped(SX(25), SY(3), SX(33), SY(12), (int)autoSrc.X, (int)autoSrc.Y, (int)autoSrc.Width, (int)autoSrc.Height);

            // Presets button
            DrawEqClamped(SX(217), SY(0), SX(44), SY(12), SX(217), SY(0), SX(44), SY(12));

            // Band slider thumbs
            int[] bandXSrc = [16, 32, 48, 64, 80, 96, 112, 128, 144, 160];

            for (int i = 0; i < 10; i++)
            {
                float val = EqBands[i];
                int thumbY = SY(17) + (int)((1f - val) * (SY(85) - SY(17)));
                DrawEqClamped(SX(bandXSrc[i]), thumbY, SX(11), SY(11), SX(13), SY(164), SX(11), SY(11));
            }

            // Preamp slider thumb
            int preampY = SY(17) + (int)((1f - EqPreamp) * (SY(85) - SY(17)));
            DrawEqClamped(SX(0), preampY, SX(11), SY(11), SX(13), SY(164), SX(11), SY(11));

            // ── EQ frequency bar display ──────────────────────────────────────
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

    // ── Playlist Window ───────────────────────────────────────────────────────

    public void DrawPlaylistWindow(int windowWidth, int windowHeight)
    {
        var dc = _surface.DeviceContext;

        // ── Draw chrome tiles from pledit.bmp ────────────────────────────────
        if (_atlas.TryGet("pledit.bmp", out var plBmp)
            && _atlas.TryGetSize("pledit.bmp", out int plW, out int plH))
        {
            // Adapt chrome layout to actual bitmap dimensions
            int topChromeH = Math.Min(20, plH / 4);       // title bar height
            int bottomChromeH = Math.Min(38, plH / 4);     // footer height
            int btnRowY = Math.Min(111, plH - 18);          // button row Y
            int cornerW = Math.Min(25, plW / 10);           // top-left corner width
            int cornerH = Math.Min(20, plH / 5);            // top-right corner height
            int rightEdgeX = Math.Max(cornerW + 1, plW - 21); // top-right corner X
            int leftEdgeW = Math.Min(12, plW / 20);         // left edge width
            int rightEdgeW = Math.Min(14, plW / 20);        // right edge width
            int bottomLeftW = Math.Min(125, plW / 2);       // bottom-left width
            int bottomRightX = Math.Min(bottomLeftW, plW - 150); // bottom-right X
            int bottomRightW = Math.Min(150, plW - bottomRightX); // bottom-right width

            int contentTop    = topChromeH;
            int contentBottom = windowHeight - bottomChromeH;

            // Helper: draw a sprite with source rect clamped to bitmap bounds
            void DrawClamped(int dstX, int dstY, int dstW, int dstH, int srcX, int srcY, int srcW, int srcH)
            {
                // Clamp source to bitmap bounds
                if (srcX + srcW > plW) srcW = Math.Max(0, plW - srcX);
                if (srcY + srcH > plH) srcH = Math.Max(0, plH - srcY);
                if (srcW <= 0 || srcH <= 0) return;

                dc.DrawBitmap(plBmp,
                    new Rect(dstX, dstY, dstX + dstW, dstY + dstH), 1f,
                    BitmapInterpolationMode.NearestNeighbor,
                    new Rect(srcX, srcY, srcX + srcW, srcY + srcH));
            }

            // Top-left corner
            DrawClamped(0, 0, cornerW, topChromeH, 0, 0, cornerW, topChromeH);

            // Top tile (repeat horizontally) — use 1px slice
            DrawClamped(cornerW, 0, windowWidth - cornerW - (plW - rightEdgeX), topChromeH,
                cornerW, 0, 1, topChromeH);

            // Top-right corner
            DrawClamped(windowWidth - (plW - rightEdgeX), 0, plW - rightEdgeX, topChromeH,
                rightEdgeX, 0, plW - rightEdgeX, topChromeH);

            // Left edge (tile vertically) — use 1px slice
            int edgeH = contentBottom - contentTop;
            if (edgeH > 0)
                DrawClamped(0, contentTop, leftEdgeW, edgeH, 0, topChromeH, leftEdgeW, 1);

            // Right edge (tile vertically) — use 1px slice
            if (edgeH > 0)
                DrawClamped(windowWidth - rightEdgeW, contentTop, rightEdgeW, edgeH,
                    plW - rightEdgeW, topChromeH, rightEdgeW, 1);

            // Scrollbar track (tile vertically on right side)
            int sbX = windowWidth - rightEdgeW;
            if (edgeH > 0)
                DrawClamped(sbX, contentTop, rightEdgeW, edgeH,
                    plW - rightEdgeW, topChromeH, rightEdgeW, 1);

            // Bottom-left corner
            int bottomH = windowHeight - contentBottom;
            DrawClamped(0, contentBottom, bottomLeftW, bottomH,
                0, btnRowY - 38, bottomLeftW, Math.Min(bottomChromeH, plH - (btnRowY - 38)));

            // Bottom-right corner
            DrawClamped(windowWidth - bottomRightW, contentBottom, bottomRightW, bottomH,
                bottomRightX, btnRowY - 38, bottomRightW,
                Math.Min(bottomChromeH, plH - (btnRowY - 38)));

            // Buttons row at bottom
            int btnW = Math.Min(21, plW / 5);
            int btnGap = Math.Min(28, btnW + 7);
            int btnH = Math.Min(9, bottomChromeH / 4);
            for (int b = 0; b < 4; b++)
            {
                int btnX = b * btnGap;
                int srcBtnX = b * btnGap;
                if (srcBtnX + btnW <= plW && btnRowY + btnH <= plH)
                    DrawClamped(btnX, windowHeight - btnH - 1, btnW, btnH,
                        srcBtnX, btnRowY, btnW, btnH);
            }

            // Scrollbar thumb
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
            // No pledit bitmap — draw solid dark background
            using var brush = dc.CreateSolidColorBrush(new Color4(0.1f, 0.1f, 0.15f, 1f));
            dc.FillRectangle(new Rect(0, 0, windowWidth, windowHeight), brush);
        }

        // ── Track list text ──────────────────────────────────────────────────
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

        // Use colors from pledit.txt if available, otherwise use defaults
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

    // ── Sprite ────────────────────────────────────────────────────────────────

    private void DrawSprite(LayerDef layer)
    {
        if (layer.Source is null || layer.SrcRect is null) return;
        if (!_atlas.TryGet(layer.Source, out var bmp)) return;

        var src = ResolveStateRect(layer);

        // Clamp source rect to actual bitmap dimensions
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

    // ── Slider ────────────────────────────────────────────────────────────────

    private void DrawSlider(LayerDef layer)
    {
        // Determine if this is a vertical slider (volume/balance) or horizontal (seekbar)
        bool isVertical = layer.Bind == "volume" || layer.Bind == "balance";

        if (layer.Track?.Source is not null && layer.Track.SrcRect is not null
            && _atlas.TryGet(layer.Track.Source, out var trackBmp))
        {
            var s = layer.Track.SrcRect;

            if (isVertical)
            {
                // For vertical sliders: draw the active slice (at the current level) at the destination
                int level = layer.Bind == "volume" ? VolumeLevel : BalanceLevel;
                int sliceH = 15;
                int sliceY = level * sliceH;
                int srcW = Math.Min(s.W, 68);
                int srcH = Math.Min(sliceH, Math.Max(0, (layer.Track?.SrcRect?.H ?? 420) - sliceY));

                var dst = new Rect(layer.Dst[0], layer.Dst[1], layer.Dst[0] + srcW, layer.Dst[1] + sliceH);
                var src = new Rect(s.X, sliceY, s.X + srcW, sliceY + srcH);
                _surface.DeviceContext.DrawBitmap(trackBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
            else
            {
                // Horizontal slider: draw the full track
                var dst = new Rect(layer.Dst[0], layer.Dst[1],
                                   layer.Dst[0] + s.W, layer.Dst[1] + s.H);
                var src = new Rect(s.X, s.Y, s.X + s.W, s.Y + s.H);
                _surface.DeviceContext.DrawBitmap(trackBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
        }

        if (layer.Thumb?.Source is not null && layer.Thumb.SrcRect is not null
            && _atlas.TryGet(layer.Thumb.Source, out var thumbBmp))
        {
            var s = layer.Thumb.SrcRect;

            if (isVertical)
            {
                // Vertical slider: thumb position based on level (0-27)
                // Each position is 15px tall in the sprite (SpriteRegions.Volume.SliceHeight)
                int level = layer.Bind == "volume" ? VolumeLevel : BalanceLevel;
                int sliceH = 15; // standard slice height for volume/balance
                int thumbH = s.H;

                // Calculate total travel range from track src rect height
                int trackH = layer.Track?.SrcRect?.H ?? (28 * sliceH);
                int travelRange = trackH - thumbH;
                int thumbY = layer.Dst[1] + (travelRange * (27 - level) / 27);

                var dst = new Rect(layer.Dst[0], thumbY, layer.Dst[0] + s.W, thumbY + thumbH);
                var src = new Rect(s.X, s.Y, s.X + s.W, s.Y + s.H);
                _surface.DeviceContext.DrawBitmap(thumbBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
            else
            {
                // Horizontal slider (seekbar)
                double pos = layer.Bind == "playback.position" ? PlaybackPosition : 0.5;
                int trackW = layer.Track?.SrcRect?.W ?? 248;
                int thumbW = layer.Thumb.SrcRect.W;
                int thumbX = layer.Dst[0] + (int)((trackW - thumbW) * pos);
                int thumbY = layer.Dst[1];

                var dst = new Rect(thumbX, thumbY, thumbX + s.W, thumbY + s.H);
                var src = new Rect(s.X, s.Y, s.X + s.W, s.Y + s.H);
                _surface.DeviceContext.DrawBitmap(thumbBmp, dst, 1f,
                    BitmapInterpolationMode.NearestNeighbor, src);
            }
        }
    }

    // ── Digit readout ─────────────────────────────────────────────────────────

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
            Rectangle src = d.HasValue
                ? SpriteRegions.Numbers.Digit(d.Value)
                : SpriteRegions.Numbers.Minus;

            var dst = new Rect(x, y, x + dw, y + dh);
            var srcF = new Rect(src.X, src.Y, src.X + src.Width, src.Y + src.Height);
            _surface.DeviceContext.DrawBitmap(bmp, dst, 1f,
                BitmapInterpolationMode.NearestNeighbor, srcF);
            x += dw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Color4 GetEqBarColor(int bandIndex, float value)
    {
        // Use viscolor.txt colors if available
        if (VisColors is { Length: > 0 })
        {
            // Map band+value to a viscolor index (indices 0-15)
            int colorIdx = Math.Clamp((int)(value * (VisColors.Length - 1)), 0, VisColors.Length - 1);
            uint c = VisColors[colorIdx];
            return new Color4(
                ((c >> 16) & 0xFF) / 255f,
                ((c >> 8) & 0xFF) / 255f,
                (c & 0xFF) / 255f, 1f);
        }

        // Default: blue gradient based on value
        float r = 0.1f + value * 0.1f;
        float g = 0.2f + value * 0.3f;
        float b = 0.6f + value * 0.4f;
        return new Color4(r, g, b, 1f);
    }

    private Rectangle ResolveStateRect(LayerDef layer)
    {
        if (!TitleBarActive
            && layer.Id == "titlebar"
            && layer.States?.TryGetValue("inactive", out var s) == true
            && s.SrcRect is not null)
        {
            return s.SrcRect.ToRectangle();
        }
        return layer.SrcRect?.ToRectangle() ?? Rectangle.Empty;
    }
}
