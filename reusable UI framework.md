# Thinking in Terms of Behavior, Not Winamp Controls

Instead of thinking in terms of **Winamp controls**, think in terms of
**behavior**.

The Winamp skin is only a visual theme. The engine should understand
generic UI behaviors that can be reused by any application.

## Behavior Groups

  ------------------------------------------------------------------------
  Behavior         Generic Control             Winamp Examples
  ---------------- --------------------------- ---------------------------
  Clickable        Button                      Play, Pause, Stop, Next,
                                               Previous, Eject, EQ,
                                               Playlist, Close, Minimize

  Toggle           ToggleButton                Shuffle, Repeat, Mono,
                                               Stereo, EQ On/Off

  Continuous Value Slider                      Volume, Balance, Seek, EQ
                                               Bands, Preamp

  Text Display     Label                       Song Title, Time, Bitrate,
                                               Sample Rate

  Animated Display Visualizer                  Spectrum Analyzer,
                                               Oscilloscope, VU Meter

  Progress         ProgressBar                 Seek Progress, Loading

  Selection        ListView                    Playlist

  Container        Panel / Window              Main Window, EQ Window,
                                               Playlist Window

  Window Control   WindowFrame                 Drag Area, Resize Area

  Menu             Menu                        Context Menu

  Static Image     Image                       Logos, Decorations

  Indicator        Indicator                   Stereo LED, Mono LED, Play
                                               Status

  Text Input       TextBox                     Search, Rename

  Scrollable Area  ScrollView                  Playlist
  ------------------------------------------------------------------------

## Engine Hierarchy

``` text
Control
│
├── Button
│     ├── PushButton
│     └── ToggleButton
│
├── ValueControl
│     ├── Slider
│     ├── Knob
│     └── ProgressBar
│
├── Display
│     ├── Label
│     ├── Image
│     ├── Indicator
│     └── Visualizer
│
├── Container
│     ├── Window
│     ├── Panel
│     └── ScrollView
│
├── Selection
│     ├── ListView
│     ├── Menu
│     └── Tabs
│
└── Input
      ├── TextBox
      └── CheckBox
```

## Generic Button

``` text
Button
    Normal Bitmap
    Hover Bitmap
    Pressed Bitmap
    Disabled Bitmap

    HitTest()
    Draw()
    OnMouseDown()
    OnMouseUp()
    OnClick()
```

Every clickable object (Play, Stop, Close, Next...) is simply a Button
with different artwork and events.

## Generic Slider

``` text
Slider
    Track Bitmap
    Thumb Bitmap

    Minimum
    Maximum
    Value

    HitTest()
    Draw()
    OnDrag()
    OnValueChanged()
```

Volume, Balance, Seek, EQ Bands and Preamp are all Slider instances.

## Generic Indicator

``` text
Indicator
    On Bitmap
    Off Bitmap

    State

    Draw()
```

Stereo, Mono, Shuffle and Repeat are all Indicators or ToggleButtons
depending on whether they accept user interaction.

## Mapping Layer

The skin should only describe how controls are arranged.

``` text
PlayButton      -> Button
PauseButton     -> Button
NextButton      -> Button
Volume          -> Slider
Balance         -> Slider
Seek            -> Slider
SongTitle       -> Label
Time            -> Label
Spectrum        -> Visualizer
Playlist        -> ListView
```

## Design Principle

-   Behavior belongs to the engine.
-   Appearance belongs to the skin.
-   Layout belongs to the skin definition.
-   Controls should never know they are "Winamp" controls.
-   Every control should be reusable in another application simply by
    changing its bitmap resources and configuration.

This approach turns the project from a Winamp clone into a reusable
bitmap-based UI engine.
