# WinampSkinEngine

A bare Direct2D / Direct3D 11 skin engine for Winamp 5 Classic `.wsz` skins,
built on .NET 8 + WinForms (HWND only) + Vortice.Windows.

## Architecture

```
WinForms Form  ──► HWND + message pump only (no GDI painting)
     │
     ▼
D2DDeviceResources  ──► D3D11 device + DXGI SwapChain (CreateSwapChainForHwnd)
                              └─► ID2D1DeviceContext5
                                       │
                              SkinAtlas (ID2D1Bitmap1 per WSZ bitmap)
                                       │
                              SkinRenderer  ──► DrawBitmap per layer-tree node
                                       ▲
                              SkinDefinition (JSON layer tree — ClassicSilver built-in)
                                       ▲
                              WszSkinLoader  ──► ZipFile → BMP decode → ChromaKey pass
```

## Prerequisites

- Windows 10/11 x64
- .NET 8 SDK  (`winget install Microsoft.DotNet.SDK.8`)
- Visual Studio 2022 or `dotnet` CLI

## Build

```bash
cd WinampSkinEngine
dotnet restore
dotnet build -c Release
```

## Run

```bash
# No skin (black window, demonstrates render pipeline)
dotnet run

# With a .wsz skin file
dotnet run -- "C:\path\to\classic_silver.wsz"
```

Or from the publish output:
```
WinampSkinEngine.exe "C:\Skins\classic_silver.wsz"
```

## Hot-Reload

Edit and save the `.wsz` file while the app is running.
The `SkinHotReloader` FileSystemWatcher picks up the change within ~300 ms and
swaps the GPU atlas on the next render frame — no restart needed.

## Key Files

| File | Purpose |
|------|---------|
| `Core/ChromaKey.cs` | Strips magenta `(255,0,255)` chroma-key transparency |
| `Skin/WszSkinLoader.cs` | ZIP → decode BMP → `RawBitmapData` |
| `Skin/SpriteRegions.cs` | Fixed pixel coordinate table for all classic WSZ sprites |
| `Skin/SkinLayerDefinition.cs` | JSON layer-tree types + Classic Silver built-in definition |
| `Skin/SkinHotReloader.cs` | `FileSystemWatcher` → debounced reload on render thread |
| `Rendering/D2DDeviceResources.cs` | D3D11 + DXGI swap chain + D2D device context |
| `Rendering/SkinAtlas.cs` | GPU bitmap dictionary (`ID2D1Bitmap1`) |
| `Rendering/SkinRenderer.cs` | Layer-tree interpreter → D2D `DrawBitmap` calls |
| `UI/PlayerWindow.cs` | WinForms HWND shell + 60 fps render loop |

## Extending

To add a new window (EQ, Playlist):
1. Add entries to `SkinDefinition.BuildClassicSilver()` under `"eq"` / `"playlist"`.
2. Call `_renderer.DrawWindow("eq")` from `PlayerWindow`.
3. Create a second `Form` (HWND) and a second `D2DDeviceResources` for it,
   or share the D2D device and draw to a second swap chain.

## Notes

- All D2D bitmaps use `B8G8R8A8_UNorm` + premultiplied alpha.
- Chroma-key (magenta) conversion happens *before* GPU upload — no shader needed.
- The `SkinDefinition` JSON format is intentionally minimal; extend `LayerDef`
  for animations, tweening, or custom control types as needed.
