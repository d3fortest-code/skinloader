namespace SkinEngine.Core.Rendering;
using Vortice.Direct2D1;
using Vortice.DXGI;

using SkinEngine.Core;
using Vortice.DCommon;
using Vortice.Mathematics;

/// <summary>
/// Manages a keyed dictionary of <see cref="ID2D1Bitmap1"/> objects
/// uploaded from the decoded <see cref="SkinEngine.Core.Skin.WszSkin"/> bitmaps.
///
/// All operations must happen on the render thread (the thread that owns the D2D device context).
/// </summary>
public sealed class SkinAtlas : IDisposable
{
    private readonly ID2D1DeviceContext5 _dc;
    private readonly Dictionary<string, ID2D1Bitmap1> _bitmaps = new();
    private bool _disposed;

    public int Count => _bitmaps.Count;

    public SkinAtlas(ID2D1DeviceContext5 dc) => _dc = dc;

    public unsafe void Upload(string key, RawBitmapData raw)
    {
        if (_bitmaps.TryGetValue(key, out var old))
        {
            old.Dispose();
            _bitmaps.Remove(key);
        }

        var props = new BitmapProperties1(
            new PixelFormat(
                Format.B8G8R8A8_UNorm,
                Vortice.DCommon.AlphaMode.Premultiplied),
            96.0f,
            96.0f,
            BitmapOptions.None);

        fixed (byte* p = raw.Pixels)
        {
            var bitmap = _dc.CreateBitmap(
                new SizeI((int)raw.Width, (int)raw.Height),
                (nint)p,
                (uint)(raw.Width * 4),
                props);

            _bitmaps[key] = bitmap;
            AppLogger.Info($"[SkinAtlas] Uploaded '{key}' — {raw.Width}x{raw.Height}, {raw.Pixels.Length} bytes");
        }
    }

    public void UploadSkin(Skin.WszSkin skin)
    {
        AppLogger.Info("[SkinAtlas] Uploading skin bitmaps...");
        int uploaded = 0;

        UploadIfNotNull("main.bmp",       skin.MainBitmap);       if (skin.MainBitmap is not null) uploaded++;
        UploadIfNotNull("titlebar.bmp",   skin.TitleBar);         if (skin.TitleBar is not null) uploaded++;
        UploadIfNotNull("sysbuttons.bmp", skin.SysButtons);       if (skin.SysButtons is not null) uploaded++;
        UploadIfNotNull("cbuttons.bmp",   skin.CButtons);         if (skin.CButtons is not null) uploaded++;
        UploadIfNotNull("posbar.bmp",     skin.PosBar);           if (skin.PosBar is not null) uploaded++;
        UploadIfNotNull("volume.bmp",     skin.Volume);           if (skin.Volume is not null) uploaded++;
        UploadIfNotNull("balance.bmp",    skin.Balance);          if (skin.Balance is not null) uploaded++;
        UploadIfNotNull("numbers.bmp",    skin.Numbers);          if (skin.Numbers is not null) uploaded++;
        UploadIfNotNull("text.bmp",       skin.TextSheet);        if (skin.TextSheet is not null) uploaded++;
        UploadIfNotNull("monoster.bmp",   skin.MonoStereo);       if (skin.MonoStereo is not null) uploaded++;
        UploadIfNotNull("playpaus.bmp",   skin.PlayPause);        if (skin.PlayPause is not null) uploaded++;
        UploadIfNotNull("pledit.bmp",     skin.PlEdit);           if (skin.PlEdit is not null) uploaded++;
        UploadIfNotNull("shufrep.bmp",    skin.ShufRep);          if (skin.ShufRep is not null) uploaded++;
        UploadIfNotNull("eqmain.bmp",     skin.EqMain);           if (skin.EqMain is not null) uploaded++;
        UploadIfNotNull("eqex.bmp",       skin.EqEx);             if (skin.EqEx is not null) uploaded++;

        AppLogger.Info($"[SkinAtlas] Upload complete — {uploaded}/15 bitmaps uploaded, {_bitmaps.Count} total in atlas");
    }

    private void UploadIfNotNull(string key, RawBitmapData? raw)
    {
        if (raw is not null) Upload(key, raw);
        else AppLogger.Info($"[SkinAtlas] Skipping '{key}' — null bitmap in skin");
    }

    public ID2D1Bitmap1? Get(string key) =>
        _bitmaps.TryGetValue(key, out var b) ? b : null;

    public bool TryGet(string key, out ID2D1Bitmap1 bitmap) =>
        _bitmaps.TryGetValue(key, out bitmap!);

    public bool TryGetSize(string key, out int width, out int height)
    {
        if (_bitmaps.TryGetValue(key, out var bmp))
        {
            var sz = bmp.PixelSize;
            width  = sz.Width;
            height = sz.Height;
            return true;
        }
        width = height = 0;
        return false;
    }

    public void DisposeAll()
    {
        AppLogger.Info($"[SkinAtlas] Disposing {_bitmaps.Count} bitmaps...");
        foreach (var b in _bitmaps.Values) b.Dispose();
        _bitmaps.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeAll();
        GC.SuppressFinalize(this);
    }
}