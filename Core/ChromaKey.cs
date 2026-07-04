namespace WinampSkinEngine.Core;

/// <summary>
/// Classic Winamp skins use magenta (R=255, G=0, B=255) as a chroma-key
/// for transparency instead of a real alpha channel.
/// This pass converts every magenta pixel to fully-transparent premultiplied black,
/// and every opaque pixel to A=255, so the data is ready for Direct2D upload.
/// </summary>
public static class ChromaKey
{
    /// <summary>
    /// Modifies <paramref name="bgra"/> in-place.
    /// Expected layout per pixel: [B, G, R, A] (4 bytes).
    /// </summary>
    public static void Strip(Span<byte> bgra)
    {
        if (bgra.Length % 4 != 0)
            throw new ArgumentException("Buffer length must be a multiple of 4.", nameof(bgra));

        for (int i = 0; i < bgra.Length; i += 4)
        {
            byte b = bgra[i];
            byte g = bgra[i + 1];
            byte r = bgra[i + 2];

            if (r == 255 && g == 0 && b == 255)
            {
                // Magenta → fully transparent premultiplied black
                bgra[i]     = 0;
                bgra[i + 1] = 0;
                bgra[i + 2] = 0;
                bgra[i + 3] = 0;
            }
            else
            {
                // Fully opaque — premultiplied RGB == straight RGB when A=255
                bgra[i + 3] = 255;
            }
        }
    }

    /// <summary>Convenience overload for a byte array.</summary>
    public static void Strip(byte[] bgra) => Strip(bgra.AsSpan());
}
