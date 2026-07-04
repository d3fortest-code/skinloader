namespace WinampSkinEngine.Rendering;

using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Direct3D11;

/// <summary>
/// Legacy wrapper that owns a <see cref="D2DSharedDevice"/> and a <see cref="D2DWindowSurface"/>
/// for backwards compatibility. New code should use those classes directly.
/// </summary>
public sealed class D2DDeviceResources : IDisposable
{
    public ID2D1DeviceContext5 DeviceContext => _surface.DeviceContext;
    public IDXGISwapChain1     SwapChain     => _surface.SwapChain;
    public int Width  => _surface.Width;
    public int Height => _surface.Height;

    public D2DSharedDevice  SharedDevice => _shared;
    public D2DWindowSurface Surface     => _surface;

    private D2DSharedDevice  _shared = null!;
    private D2DWindowSurface _surface = null!;
    private bool _disposed;

    /// <summary>
    /// Create using a new private <see cref="D2DSharedDevice"/>.
    /// </summary>
    public static D2DDeviceResources Create(IntPtr hwnd, int width, int height)
    {
        var shared  = D2DSharedDevice.Create();
        var surface = shared.CreateWindowSurface(hwnd, width, height);
        return new D2DDeviceResources(shared, surface);
    }

    /// <summary>
    /// Create using an existing shared device (for multi-window scenarios).
    /// </summary>
    public static D2DDeviceResources Create(D2DSharedDevice shared, IntPtr hwnd, int width, int height)
    {
        var surface = shared.CreateWindowSurface(hwnd, width, height);
        return new D2DDeviceResources(shared, surface);
    }

    private D2DDeviceResources(D2DSharedDevice shared, D2DWindowSurface surface)
    {
        _shared  = shared;
        _surface = surface;
    }

    public void Resize(int newWidth, int newHeight) =>
        _surface.Resize(newWidth, newHeight);

    public void BeginDraw() => _surface.BeginDraw();
    public void EndDraw()   => _surface.EndDraw();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _surface.Dispose();
        _shared.Dispose();

        GC.SuppressFinalize(this);
    }
}
