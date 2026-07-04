namespace WinampSkinEngine.Skin;

using System.Drawing;
using WinampSkinEngine.Core;

/// <summary>
/// Parsed contents of pledit.txt — playlist font, text color, background color, etc.
/// </summary>
public sealed class PleditConfig
{
    /// <summary>Normal text color (24-bit RGB, e.g. 0x6E6E96).</summary>
    public uint TextColor      { get; set; } = 0x6E6E96;

    /// <summary>Current/playing track text color.</summary>
    public uint CurrentColor   { get; set; } = 0xAAAAC8;

    /// <summary>Background color.</summary>
    public uint BgColor        { get; set; } = 0x000000;

    /// <summary>Selected track background color.</summary>
    public uint SelectedBgColor { get; set; } = 0x3C3C6E;

    /// <summary>Font name (e.g. "Arial").</summary>
    public string FontName     { get; set; } = "Arial";

    /// <summary>Mini-browser background color.</summary>
    public uint MbBgColor      { get; set; }

    /// <summary>Mini-browser foreground color.</summary>
    public uint MbFgColor      { get; set; } = 0x6E6E96;
}

/// <summary>
/// Plain-data bag that holds every decoded bitmap and metadata
/// extracted from a .wsz (Winamp skin ZIP) archive.
/// All bitmaps have already had the chroma-key pass applied.
/// </summary>
public sealed class WszSkin : IDisposable
{
    // ── Main window ──────────────────────────────────────────────────────────
    /// <summary>main.bmp — 275×116 px full window background.</summary>
    public RawBitmapData? MainBitmap    { get; set; }

    // ── Title bar ────────────────────────────────────────────────────────────
    /// <summary>titlebar.bmp — active (y=0) and inactive (y=15) bars.</summary>
    public RawBitmapData? TitleBar      { get; set; }

    // ── System buttons (min / shade / close) ─────────────────────────────────
    /// <summary>sysbuttons.bmp</summary>
    public RawBitmapData? SysButtons    { get; set; }

    // ── Transport controls ───────────────────────────────────────────────────
    /// <summary>cbuttons.bmp — 136×36 px sprite sheet (normal row y=0, pressed row y=18).</summary>
    public RawBitmapData? CButtons      { get; set; }

    // ── Seek / position bar ───────────────────────────────────────────────────
    /// <summary>posbar.bmp — track + thumb sprites.</summary>
    public RawBitmapData? PosBar        { get; set; }

    // ── Volume and balance sliders ────────────────────────────────────────────
    /// <summary>volume.bmp — 28 vertical positions.</summary>
    public RawBitmapData? Volume        { get; set; }

    /// <summary>balance.bmp — 28 vertical positions.</summary>
    public RawBitmapData? Balance       { get; set; }

    // ── Time display ──────────────────────────────────────────────────────────
    /// <summary>numbers.bmp — 9×13 px digit sprites 0–9 plus minus.</summary>
    public RawBitmapData? Numbers       { get; set; }

    // ── Scrolling title text ──────────────────────────────────────────────────
    /// <summary>text.bmp — character sprite sheet for the scrolling title.</summary>
    public RawBitmapData? TextSheet     { get; set; }

    // ── Mono / stereo indicators ──────────────────────────────────────────────
    /// <summary>monoster.bmp</summary>
    public RawBitmapData? MonoStereo    { get; set; }

    // ── Play / pause overlay ──────────────────────────────────────────────────
    /// <summary>playpaus.bmp</summary>
    public RawBitmapData? PlayPause     { get; set; }

    // ── Playlist editor chrome ────────────────────────────────────────────────
    /// <summary>pledit.bmp</summary>
    public RawBitmapData? PlEdit        { get; set; }

    // ── Shuffle / repeat / EQ / PL buttons ───────────────────────────────────
    /// <summary>shufrep.bmp</summary>
    public RawBitmapData? ShufRep       { get; set; }

    // ── Equaliser windows ─────────────────────────────────────────────────────
    /// <summary>eq_main.bmp</summary>
    public RawBitmapData? EqMain        { get; set; }

    /// <summary>eq_ex.bmp</summary>
    public RawBitmapData? EqEx          { get; set; }

    // ── Visualiser palette ────────────────────────────────────────────────────
    /// <summary>viscolor.txt — 16 ARGB colour entries.</summary>
    public uint[]? VisColors            { get; set; }

    // ── Playlist config ──────────────────────────────────────────────────────
    /// <summary>pledit.txt — font, text color, background color for playlist.</summary>
    public PleditConfig? PleditConfig   { get; set; }

    // ── Window regions ───────────────────────────────────────────────────────
    /// <summary>region.txt — polygon regions for non-rectangular windows.</summary>
    public Dictionary<string, List<Point>>? Regions { get; set; }

    // ─────────────────────────────────────────────────────────────────────────

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // RawBitmapData is a plain array wrapper — nothing to release.
        // Extend here if you later add IDisposable GPU resources.
        GC.SuppressFinalize(this);
    }
}
