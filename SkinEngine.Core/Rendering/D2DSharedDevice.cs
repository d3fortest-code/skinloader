namespace SkinEngine.Core.Rendering;

using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DCommon;
using SkinEngine.Core;

/// <summary>
/// Owns the single D3D11 device, D2D device, DXGI factory, and DirectWrite factory
/// shared across all windows. Does NOT own any swap chain or device context.
/// </summary>
public sealed class D2DSharedDevice : IDisposable
{
    public ID3D11Device      D3DDevice   { get; private set; } = null!;
    public ID2D1Device5      D2DDevice   { get; private set; } = null!;
    public IDXGIFactory2     DXGIFactory { get; private set; } = null!;
    public ID2D1Factory7     D2DFactory  { get; private set; } = null!;
    public IDWriteFactory    DWriteFactory { get; private set; } = null!;

    private bool _disposed;

    private D2DSharedDevice() { }

    public static D2DSharedDevice Create()
    {
        var dev = new D2DSharedDevice();
        dev.Initialize();
        return dev;
    }

    private void Initialize()
    {
        AppLogger.Info("[D2DSharedDevice] Creating D3D11 device (Hardware, BgraSupport)...");

        var featureLevels = new[]
        {
            Vortice.Direct3D.FeatureLevel.Level_11_1,
            Vortice.Direct3D.FeatureLevel.Level_11_0,
            Vortice.Direct3D.FeatureLevel.Level_10_1,
        };

        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            featureLevels,
            out ID3D11Device tempDevice,
            out ID3D11DeviceContext tempContext).CheckError();
        D3DDevice = tempDevice;
        tempContext.Dispose();
        AppLogger.Info("[D2DSharedDevice] D3D11 device created OK");

        AppLogger.Info("[D2DSharedDevice] Getting DXGI factory...");
        using var dxgiDevice = D3DDevice.QueryInterface<IDXGIDevice>();
        using var adapter    = dxgiDevice.GetAdapter();
        DXGIFactory = adapter.GetParent<IDXGIFactory2>();
        AppLogger.Info("[D2DSharedDevice] DXGI factory obtained");

        AppLogger.Info("[D2DSharedDevice] Creating D2D1 factory (MultiThreaded)...");
        D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory7>(
            Vortice.Direct2D1.FactoryType.MultiThreaded,
            DebugLevel.None);

        AppLogger.Info("[D2DSharedDevice] Creating D2D1 device from DXGI device...");
        D2DDevice = D2DFactory.CreateDevice(dxgiDevice);
        AppLogger.Info("[D2DSharedDevice] D2D1 device created OK");

        AppLogger.Info("[D2DSharedDevice] Creating DirectWrite factory...");
        DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
        AppLogger.Info("[D2DSharedDevice] All GPU resources initialized successfully");
    }

    public D2DWindowSurface CreateWindowSurface(IntPtr hwnd, int width, int height, float dpi = 96f)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        AppLogger.Info($"[D2DSharedDevice] Creating window surface: hwnd={hwnd}, {width}x{height}, dpi={dpi}");
        var surface = new D2DWindowSurface(D3DDevice, DXGIFactory, D2DDevice, hwnd, width, height, dpi);
        AppLogger.Info("[D2DSharedDevice] Window surface created OK");
        return surface;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AppLogger.Info("[D2DSharedDevice] Disposing GPU resources...");
        DWriteFactory?.Dispose();
        D2DDevice?.Dispose();
        D2DFactory?.Dispose();
        DXGIFactory?.Dispose();
        D3DDevice?.Dispose();
        AppLogger.Info("[D2DSharedDevice] All GPU resources disposed");

        GC.SuppressFinalize(this);
    }
}