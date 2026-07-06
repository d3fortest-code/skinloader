namespace SkinEngine.Core.Skin;

using System.Drawing;
using SkinEngine.Core;

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
    public RawBitmapData? MainBitmap    { get; set; }
    public RawBitmapData? TitleBar      { get; set; }
    public RawBitmapData? SysButtons    { get; set; }
    public RawBitmapData? CButtons      { get; set; }
    public RawBitmapData? PosBar        { get; set; }
    public RawBitmapData? Volume        { get; set; }
    public RawBitmapData? Balance       { get; set; }
    public RawBitmapData? Numbers       { get; set; }
    public RawBitmapData? TextSheet     { get; set; }
    public RawBitmapData? MonoStereo    { get; set; }
    public RawBitmapData? PlayPause     { get; set; }
    public RawBitmapData? PlEdit        { get; set; }
    public RawBitmapData? ShufRep       { get; set; }
    public RawBitmapData? EqMain        { get; set; }
    public RawBitmapData? EqEx          { get; set; }
    public uint[]? VisColors            { get; set; }
    public PleditConfig? PleditConfig   { get; set; }
    public Dictionary<string, List<Point>>? Regions { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}