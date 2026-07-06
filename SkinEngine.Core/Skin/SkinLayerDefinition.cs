namespace SkinEngine.Core.Skin;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Types of layers that can be rendered in a skin window.</summary>
public enum LayerType { Sprite, Button, Slider, DigitReadout, TextReadout }

/// <summary>Source rectangle within a bitmap (x, y, w, h).</summary>
public sealed class SrcRect
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("w")] public int W { get; set; }
    [JsonPropertyName("h")] public int H { get; set; }

    public System.Drawing.Rectangle ToRectangle() => new(X, Y, W, H);
}

/// <summary>Alternate source rectangle for a layer state (e.g., pressed, inactive).</summary>
public sealed class LayerState
{
    [JsonPropertyName("srcRect")] public SrcRect? SrcRect { get; set; }
}

/// <summary>Track definition for slider-type layers (volume, balance, seek bar).</summary>
public sealed class SliderTrack
{
    [JsonPropertyName("source")]  public string? Source  { get; set; }
    [JsonPropertyName("srcRect")] public SrcRect? SrcRect { get; set; }
}

/// <summary>
/// Defines a single renderable layer within a skin window.
/// Each layer has a source bitmap, source rectangle, destination position,
/// and optional state-dependent behavior (buttons, sliders, digit readouts).
/// </summary>
public sealed class LayerDef
{
    [JsonPropertyName("id")]        public string  Id        { get; set; } = "";
    [JsonPropertyName("type")]      public string  Type      { get; set; } = "sprite";
    [JsonPropertyName("source")]    public string? Source    { get; set; }
    [JsonPropertyName("srcRect")]   public SrcRect? SrcRect  { get; set; }
    [JsonPropertyName("dst")]       public int[]   Dst       { get; set; } = [0, 0];
    [JsonPropertyName("action")]    public string? Action    { get; set; }
    [JsonPropertyName("bind")]      public string? Bind      { get; set; }
    [JsonPropertyName("states")]    public Dictionary<string, LayerState>? States { get; set; }
    [JsonPropertyName("track")]     public SliderTrack? Track  { get; set; }
    [JsonPropertyName("thumb")]     public SliderTrack? Thumb  { get; set; }
    [JsonPropertyName("digitSize")] public int[]? DigitSize    { get; set; }
}

/// <summary>Defines a skin window (main, equalizer, playlist) with its size and layers.</summary>
public sealed class WindowDef
{
    [JsonPropertyName("size")]   public int[]     Size   { get; set; } = [275, 116];
    [JsonPropertyName("layers")] public LayerDef[] Layers { get; set; } = [];
}

/// <summary>
/// Top-level skin definition containing all windows and their layer layouts.
/// Can be loaded from JSON or built programmatically (e.g., <see cref="BuildClassicSilver"/>).
/// </summary>
public sealed class SkinDefinition
{
    [JsonPropertyName("skin")]    public string  Skin    { get; set; } = "";
    [JsonPropertyName("windows")] public Dictionary<string, WindowDef> Windows { get; set; } = new();

    public static SkinDefinition BuildClassicSilver() => new()
    {
        Skin = "winamp5-classic-silver",
        Windows = new()
        {
            ["main"] = new WindowDef
            {
                Size = [275, 116],
                Layers =
                [
                    new LayerDef { Id = "bg", Type = "sprite", Source = "main.bmp", SrcRect = new SrcRect { X=0, Y=0, W=275, H=116 }, Dst = [0, 0] },
                    new LayerDef { Id = "titlebar", Type = "sprite", Source = "titlebar.bmp", SrcRect = new SrcRect { X=0, Y=0, W=275, H=14 }, Dst = [0, 0], States = new() { ["inactive"] = new LayerState { SrcRect = new SrcRect { X=0, Y=15, W=275, H=14 } } } },
                    new LayerDef { Id = "btn_minimize", Type = "button", Source = "sysbuttons.bmp", SrcRect = new SrcRect { X= 0, Y=0, W=9, H=9 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=0, Y=9, W=9, H=9 } } }, Dst = [264, 3], Action = "window.minimize" },
                    new LayerDef { Id = "btn_shade", Type = "button", Source = "sysbuttons.bmp", SrcRect = new SrcRect { X= 9, Y=0, W=9, H=9 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=9, Y=9, W=9, H=9 } } }, Dst = [244, 3], Action = "window.shade" },
                    new LayerDef { Id = "btn_close", Type = "button", Source = "sysbuttons.bmp", SrcRect = new SrcRect { X=18, Y=0, W=9, H=9 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=18, Y=9, W=9, H=9 } } }, Dst = [254, 3], Action = "window.close" },
                    new LayerDef { Id = "play_pause", Type = "sprite", Source = "playpaus.bmp", SrcRect = new SrcRect { X=0, Y=0, W=9, H=9 }, Dst = [16, 28] },
                    new LayerDef { Id = "mono_stereo", Type = "sprite", Source = "monoster.bmp", SrcRect = new SrcRect { X=0, Y=24, W=29, H=12 }, Dst = [211, 40] },
                    new LayerDef { Id = "btn_prev", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X= 0, Y= 0, W=23, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=0, Y=18, W=23, H=18 } } }, Dst = [16, 88], Action = "transport.prev" },
                    new LayerDef { Id = "btn_play", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X=23, Y= 0, W=23, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=23, Y=18, W=23, H=18 } } }, Dst = [39, 88], Action = "transport.play" },
                    new LayerDef { Id = "btn_pause", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X=46, Y= 0, W=23, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=46, Y=18, W=23, H=18 } } }, Dst = [62, 88], Action = "transport.pause" },
                    new LayerDef { Id = "btn_stop", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X=69, Y= 0, W=23, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=69, Y=18, W=23, H=18 } } }, Dst = [85, 88], Action = "transport.stop" },
                    new LayerDef { Id = "btn_next", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X=92, Y= 0, W=22, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=92, Y=18, W=22, H=18 } } }, Dst = [108, 88], Action = "transport.next" },
                    new LayerDef { Id = "btn_eject", Type = "button", Source = "cbuttons.bmp", SrcRect = new SrcRect { X=114, Y= 0, W=22, H=18 }, States = new() { ["pressed"] = new LayerState { SrcRect = new SrcRect { X=114, Y=18, W=22, H=18 } } }, Dst = [136, 88], Action = "transport.eject" },
                    new LayerDef { Id = "volume", Type = "slider", Track = new SliderTrack { Source = "volume.bmp", SrcRect = new SrcRect { X=0, Y=0, W=68, H=420 } }, Thumb = new SliderTrack { Source = "volume.bmp", SrcRect = new SrcRect { X=0, Y=0, W=14, H=11 } }, Dst = [107, 57], Bind = "volume" },
                    new LayerDef { Id = "balance", Type = "slider", Track = new SliderTrack { Source = "balance.bmp", SrcRect = new SrcRect { X=0, Y=0, W=38, H=420 } }, Thumb = new SliderTrack { Source = "balance.bmp", SrcRect = new SrcRect { X=0, Y=0, W=14, H=11 } }, Dst = [177, 57], Bind = "balance" },
                    new LayerDef { Id = "seekbar", Type = "slider", Track = new SliderTrack { Source = "posbar.bmp", SrcRect = new SrcRect { X=  0, Y=0, W=248, H=10 } }, Thumb = new SliderTrack { Source = "posbar.bmp", SrcRect = new SrcRect { X=248, Y=0, W= 29, H=10 } }, Dst = [16, 72], Bind = "playback.position" },
                    new LayerDef { Id = "time_display", Type = "digit_readout", Source = "numbers.bmp", DigitSize = [9, 13], Dst = [48, 26], Bind = "playback.elapsed" }
                ]
            }
        }
    };

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}