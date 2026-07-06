namespace SkinEngine.Core.Skin;

using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using SkinEngine.Core;

/// <summary>
/// Loads and decodes .wsz (Winamp skin ZIP) archives into <see cref="WszSkin"/> objects.
/// Handles BMP decoding, chroma-key transparency, and parsing of viscolor.txt, pledit.txt, and region.txt.
/// </summary>
public static class WszSkinLoader
{
    private static string NormalizeKey(string filename)
    {
        return filename.ToLowerInvariant().Replace("_", "").Replace("-", "");
    }

    /// <summary>Loads a .wsz skin archive from the given file path.</summary>
    public static WszSkin Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Skin file not found: {path}", path);

        AppLogger.Info($"[WszSkinLoader] Opening skin: {path}");
        var skin = new WszSkin();

        using var zip = ZipFile.OpenRead(path);
        AppLogger.Info($"[WszSkinLoader] ZIP opened — {zip.Entries.Count} entries");

        int decoded = 0;
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            string key = NormalizeKey(entry.Name);

            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;

            switch (key)
            {
                case "main.bmp":       skin.MainBitmap  = DecodeBitmap(ms, key); decoded++; break;
                case "titlebar.bmp":   skin.TitleBar    = DecodeBitmap(ms, key); decoded++; break;
                case "sysbuttons.bmp": skin.SysButtons  = DecodeBitmap(ms, key); decoded++; break;
                case "cbuttons.bmp":   skin.CButtons    = DecodeBitmap(ms, key); decoded++; break;
                case "posbar.bmp":
                case "posbar2.bmp":
                    var posData = DecodeBitmap(ms, key);
                    if (skin.PosBar is null || (skin.PosBar.Width <= 1 && posData.Width > 1))
                        skin.PosBar = posData;
                    decoded++;
                    break;
                case "volume.bmp":     skin.Volume      = DecodeBitmap(ms, key); decoded++; break;
                case "balance.bmp":    skin.Balance     = DecodeBitmap(ms, key); decoded++; break;
                case "numbers.bmp":    skin.Numbers     = DecodeBitmap(ms, key); decoded++; break;
                case "text.bmp":       skin.TextSheet   = DecodeBitmap(ms, key); decoded++; break;
                case "monoster.bmp":   skin.MonoStereo  = DecodeBitmap(ms, key); decoded++; break;
                case "playpaus.bmp":   skin.PlayPause   = DecodeBitmap(ms, key); decoded++; break;
                case "pledit.bmp":     skin.PlEdit      = DecodeBitmap(ms, key); decoded++; break;
                case "eqmain.bmp":     skin.EqMain      = DecodeBitmap(ms, key); decoded++; break;
                case "eqex.bmp":       skin.EqEx        = DecodeBitmap(ms, key); decoded++; break;
                case "shufrep.bmp":    skin.ShufRep     = DecodeBitmap(ms, key); decoded++; break;
                case "viscolor.txt":   skin.VisColors   = ParseVisColors(ms); AppLogger.Info("[WszSkinLoader] Parsed viscolor.txt"); break;
                case "pledit.txt":     skin.PleditConfig = ParsePleditConfig(ms); AppLogger.Info("[WszSkinLoader] Parsed pledit.txt"); break;
                case "region.txt":     skin.Regions     = ParseRegionTxt(ms); AppLogger.Info("[WszSkinLoader] Parsed region.txt"); break;
            }
        }

        AppLogger.Info($"[WszSkinLoader] Load complete — {decoded} bitmaps decoded from {zip.Entries.Count} entries");
        return skin;
    }

    private static RawBitmapData DecodeBitmap(Stream stream, string key)
    {
        using var bmp = new Bitmap(stream);
        AppLogger.Info($"[WszSkinLoader] Decoded '{key}' — {bmp.Width}x{bmp.Height} ({bmp.RawFormat})");

        using var converted = bmp.Clone(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            PixelFormat.Format32bppArgb) as Bitmap
            ?? throw new InvalidOperationException("Bitmap.Clone returned null.");

        var rect = new Rectangle(0, 0, converted.Width, converted.Height);
        var data = converted.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int byteCount = Math.Abs(data.Stride) * converted.Height;
            byte[] pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, byteCount);

            ChromaKey.Strip(pixels);

            return new RawBitmapData(converted.Width, converted.Height, pixels);
        }
        finally
        {
            converted.UnlockBits(data);
        }
    }

    private static uint[] ParseVisColors(Stream stream)
    {
        var colors = new uint[16];
        int index = 0;

        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        string? line;

        while ((line = reader.ReadLine()) != null && index < 16)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';')) continue;

            var parts = line.Split(new char[] { ',', ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3
                && byte.TryParse(parts[0], out byte r)
                && byte.TryParse(parts[1], out byte g)
                && byte.TryParse(parts[2], out byte b))
            {
                colors[index++] = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
            }
        }

        for (; index < 16; index++)
            colors[index] = 0xFF000000u;

        return colors;
    }

    private static PleditConfig ParsePleditConfig(Stream stream)
    {
        var config = new PleditConfig();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('[') || line.StartsWith(';'))
                continue;

            int eq = line.IndexOf('=');
            if (eq < 0) continue;

            string key = line[..eq].Trim();
            string val = line[(eq + 1)..].Trim();

            switch (key)
            {
                case "Normal":     config.TextColor = ParseHexColor(val); break;
                case "Current":    config.CurrentColor = ParseHexColor(val); break;
                case "NormalBG":   config.BgColor = ParseHexColor(val); break;
                case "SelectedBG": config.SelectedBgColor = ParseHexColor(val); break;
                case "Font":       config.FontName = val; break;
                case "mbBG":       config.MbBgColor = ParseHexColor(val); break;
                case "mbFG":       config.MbFgColor = ParseHexColor(val); break;
            }
        }

        AppLogger.Info($"[WszSkinLoader] PleditConfig: Font={config.FontName}, " +
            $"TextColor=#{config.TextColor:X6}, BgColor=#{config.BgColor:X6}");
        return config;
    }

    private static uint ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint val))
            return val;
        return 0;
    }

    private static Dictionary<string, List<Point>> ParseRegionTxt(Stream stream)
    {
        var regions = new Dictionary<string, List<Point>>();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        string? line;
        string? currentSection = null;
        int? numPoints = null;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].ToLowerInvariant();
                numPoints = null;
                continue;
            }

            if (currentSection is null) continue;

            var eq = line.IndexOf('=');
            if (eq < 0) continue;

            string key = line[..eq].Trim().ToLowerInvariant();
            string val = line[(eq + 1)..].Trim();

            if (key == "numpoints")
            {
                var parts = val.Split(',');
                if (int.TryParse(parts[0].Trim(), out int np))
                    numPoints = np;
            }
            else if (key == "pointlist" && numPoints.HasValue)
            {
                var points = ParsePointList(val, numPoints.Value);
                if (points.Count > 0)
                {
                    regions[currentSection] = points;
                    AppLogger.Info($"[WszSkinLoader] Region '{currentSection}': {points.Count} points parsed");
                }
            }
        }

        return regions;
    }

    private static List<Point> ParsePointList(string pointList, int expectedPoints)
    {
        var points = new List<Point>();
        var tokens = pointList.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var coords = token.Split(',');
            if (coords.Length >= 2
                && int.TryParse(coords[0].Trim(), out int x)
                && int.TryParse(coords[1].Trim(), out int y))
            {
                points.Add(new Point(x, y));
                if (points.Count >= expectedPoints) break;
            }
        }

        return points;
    }
}