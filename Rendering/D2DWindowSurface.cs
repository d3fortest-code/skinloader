namespace WinampSkinEngine.Rendering;

using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using Vortice.DCommon;
using WinampSkinEngine.Core;

/// <summary>
/// Owns a single HWND's swap chain and D2D device context.
/// One instance per window. All instances from the same
/// <see cref="D2DSharedDevice"/> share the underlying D3D11/D2D device.
/// </summary>
public sealed class D2DWindowSurface : IDisposable
{
    public ID2D1DeviceContext5 DeviceContext { get; private set; } = null!;
    public IDXGISwapChain1     SwapChain     { get; private set; } = null!;
    public int Width  { get; private set; }
    public int Height { get; private set; }

    private ID2D1Bitmap1? _targetBitmap;
    private bool _disposed;

    internal D2DWindowSurface(
        ID3D11Device d3dDevice,
        IDXGIFactory2 dxgiFactory,
        ID2D1Device5 d2dDevice,
        IntPtr hwnd,
        int width,
        int height)
    {
        Width  = width;
        Height = height;

        AppLogger.Info($"[D2DWindowSurface] Creating swap chain ({width}x{height}, B8G8R8A8_UNorm, FlipDiscard)...");

        var scDesc = new SwapChainDescription1
        {
            Width       = (uint)width,
            Height      = (uint)height,
            Format      = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            SwapEffect  = SwapEffect.FlipDiscard,
            Scaling     = Scaling.None,
            AlphaMode   = Vortice.DXGI.AlphaMode.Ignore,
        };

        SwapChain = dxgiFactory.CreateSwapChainForHwnd(d3dDevice, hwnd, scDesc);
        AppLogger.Info("[D2DWindowSurface] Swap chain created OK");

        AppLogger.Info("[D2DWindowSurface] Creating D2D device context...");
        DeviceContext = d2dDevice.CreateDeviceContext(DeviceContextOptions.None);

        AppLogger.Info("[D2DWindowSurface] Binding render target to back buffer...");
        CreateSizeDependentResources();
        AppLogger.Info("[D2DWindowSurface] Surface fully initialized");
    }

    private void CreateSizeDependentResources()
    {
        _targetBitmap?.Dispose();

        using var backBuffer = SwapChain.GetBuffer<IDXGISurface>(0);

        var props = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96f, 96f,
            BitmapOptions.Target | BitmapOptions.CannotDraw);

        _targetBitmap = DeviceContext.CreateBitmapFromDxgiSurface(backBuffer, props);
        DeviceContext.Target = _targetBitmap;
    }

    public void Resize(int newWidth, int newHeight)
    {
        if (newWidth == Width && newHeight == Height) return;
        AppLogger.Info($"[D2DWindowSurface] Resize: {Width}x{Height} → {newWidth}x{newHeight}");
        Width  = newWidth;
        Height = newHeight;

        DeviceContext.Target = null;
        _targetBitmap?.Dispose();

        SwapChain.ResizeBuffers(0, (uint)newWidth, (uint)newHeight,
            Format.B8G8R8A8_UNorm, SwapChainFlags.None).CheckError();

        CreateSizeDependentResources();
    }

    public void BeginDraw()
    {
        DeviceContext.BeginDraw();
        DeviceContext.Clear(new Color4(0, 0, 0, 1));
    }

    public void EndDraw()
    {
        DeviceContext.EndDraw();
        SwapChain.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AppLogger.Info("[D2DWindowSurface] Disposing...");
        DeviceContext.Target = null;
        _targetBitmap?.Dispose();
        DeviceContext?.Dispose();
        SwapChain?.Dispose();
        AppLogger.Info("[D2DWindowSurface] Disposed");

        GC.SuppressFinalize(this);
    }
}
