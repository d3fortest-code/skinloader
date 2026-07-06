namespace SkinEngine.Core;

/// <summary>
/// Holds raw BGRA pixel data decoded from a .wsz bitmap entry,
/// ready to upload as an ID2D1Bitmap1.
/// </summary>
public sealed class RawBitmapData
{
    public int Width  { get; }
    public int Height { get; }

    /// <summary>32-bpp BGRA bytes (Width * Height * 4). Premultiplied after chroma-key pass.</summary>
    public byte[] Pixels { get; }

    public RawBitmapData(int width, int height, byte[] pixels)
    {
        Width  = width;
        Height = height;
        Pixels = pixels;
    }
}