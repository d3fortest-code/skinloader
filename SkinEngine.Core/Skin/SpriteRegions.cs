namespace SkinEngine.Core.Skin;

using System.Drawing;

/// <summary>
/// Defines pixel-accurate source rectangles for all sprite regions in a classic Winamp skin.
/// Used by <see cref="SkinRenderer"/> to resolve which portion of a bitmap to draw
/// for each UI element state (normal, pressed, active, etc.).
/// </summary>
public static class SpriteRegions
{
    public static class CButtons
    {
        public static readonly Rectangle PrevNormal   = new( 0,  0, 23, 18);
        public static readonly Rectangle PrevPressed  = new( 0, 18, 23, 18);
        public static readonly Rectangle PlayNormal   = new(23,  0, 23, 18);
        public static readonly Rectangle PlayPressed  = new(23, 18, 23, 18);
        public static readonly Rectangle PauseNormal  = new(46,  0, 23, 18);
        public static readonly Rectangle PausePressed = new(46, 18, 23, 18);
        public static readonly Rectangle StopNormal   = new(69,  0, 23, 18);
        public static readonly Rectangle StopPressed  = new(69, 18, 23, 18);
        public static readonly Rectangle NextNormal   = new(92,  0, 22, 18);
        public static readonly Rectangle NextPressed  = new(92, 18, 22, 18);
        public static readonly Rectangle EjectNormal  = new(114,  0, 22, 18);
        public static readonly Rectangle EjectPressed = new(114, 18, 22, 18);
    }

    public static class TitleBar
    {
        public static readonly Rectangle Active   = new(0,  0, 275, 14);
        public static readonly Rectangle Inactive = new(0, 15, 275, 14);
    }

    public static class SysButtons
    {
        public static readonly Rectangle MinimizeNormal  = new( 0, 0, 9, 9);
        public static readonly Rectangle MinimizePressed = new( 0, 9, 9, 9);
        public static readonly Rectangle ShadeNormal     = new( 9, 0, 9, 9);
        public static readonly Rectangle ShadePressed    = new( 9, 9, 9, 9);
        public static readonly Rectangle CloseNormal     = new(18, 0, 9, 9);
        public static readonly Rectangle ClosePressed    = new(18, 9, 9, 9);
    }

    public static class Numbers
    {
        public const int DigitWidth  = 9;
        public const int DigitHeight = 13;

        public static Rectangle Digit(int d)
        {
            if (d < 0 || d > 9) throw new ArgumentOutOfRangeException(nameof(d));
            return new Rectangle(d * DigitWidth, 0, DigitWidth, DigitHeight);
        }

        public static readonly Rectangle Minus = new(90, 0, 9, 13);
        public static readonly Rectangle Blank = new(0, 0, 0, 0);
    }

    public static class PosBar
    {
        public static readonly Rectangle Track      = new(  0, 0, 248, 10);
        public static readonly Rectangle ThumbNormal = new(248, 0,  29, 10);
        public static readonly Rectangle ThumbDown   = new(278, 0,  29, 10);
    }

    public static class Volume
    {
        public const int Positions  = 28;
        public const int SliceHeight = 15;

        public static Rectangle Slice(int level)
        {
            if (level < 0 || level >= Positions)
                throw new ArgumentOutOfRangeException(nameof(level));
            return new Rectangle(0, level * SliceHeight, 68, SliceHeight);
        }
    }

    public static class Balance
    {
        public const int Positions   = 28;
        public const int SliceHeight = 15;

        public static Rectangle Slice(int level)
        {
            if (level < 0 || level >= Positions)
                throw new ArgumentOutOfRangeException(nameof(level));
            return new Rectangle(0, level * SliceHeight, 38, SliceHeight);
        }
    }

    public static class PlayPause
    {
        public static readonly Rectangle Playing  = new( 0, 0, 9, 9);
        public static readonly Rectangle Paused   = new( 9, 0, 9, 9);
        public static readonly Rectangle Stopped  = new(18, 0, 9, 9);
        public static readonly Rectangle Loading  = new(27, 0, 9, 9);
        public static readonly Rectangle Seeking  = new(36, 0, 9, 9);
    }

    public static class MonoStereo
    {
        public static readonly Rectangle MonoOff   = new( 0, 0, 29, 12);
        public static readonly Rectangle MonoOn    = new( 0, 12, 29, 12);
        public static readonly Rectangle StereoOff = new( 0, 24, 29, 12);
        public static readonly Rectangle StereoOn  = new( 0, 36, 29, 12);
    }

    public static class PlEdit
    {
        public static readonly Rectangle TopLeft      = new(  0,  0, 25, 20);
        public static readonly Rectangle TopTile      = new( 26,  0,  1, 20);
        public static readonly Rectangle TopRight     = new(254,  0, 21, 20);
        public static readonly Rectangle LeftEdge     = new(  0, 20, 12,  1);
        public static readonly Rectangle RightEdge    = new(261, 20, 14,  1);
        public static readonly Rectangle BottomLeft   = new(  0, 72, 125, 38);
        public static readonly Rectangle BottomRight  = new(126, 72, 150, 38);
        public static readonly Rectangle ScrollTrack  = new(261, 20, 14, 52);
        public static readonly Rectangle ScrollThumb  = new(278,  0,  8, 18);

        public static class Buttons
        {
            public static readonly Rectangle AddNormal    = new(  0, 111, 21, 9);
            public static readonly Rectangle AddHover     = new(  0, 120, 21, 9);
            public static readonly Rectangle RemoveNormal = new( 28, 111, 21, 9);
            public static readonly Rectangle RemoveHover  = new( 28, 120, 21, 9);
            public static readonly Rectangle SelectNormal = new( 56, 111, 21, 9);
            public static readonly Rectangle SelectHover  = new( 56, 120, 21, 9);
            public static readonly Rectangle MiscNormal   = new( 84, 111, 21, 9);
            public static readonly Rectangle MiscHover    = new( 84, 120, 21, 9);
        }
    }

    public static class EqMain
    {
        public static readonly Rectangle SliderThumb = new(13, 164, 11, 11);
        public static readonly int[] BandX = [16, 32, 48, 64, 80, 96, 112, 128, 144, 160];
        public const int SliderTop    = 17;
        public const int SliderBottom = 85;
        public const int SliderTravel = SliderBottom - SliderTop;
        public static readonly Rectangle PowerNormal = new(10,  3, 11, 12);
        public static readonly Rectangle PowerActive = new(10, 15, 11, 12);
        public static readonly Rectangle AutoNormal = new(25,  3, 33, 12);
        public static readonly Rectangle AutoActive = new(25, 15, 33, 12);
        public static readonly Rectangle Presets = new(217, 0, 44, 12);
    }
}