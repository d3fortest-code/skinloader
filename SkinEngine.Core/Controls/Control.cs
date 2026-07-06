namespace SkinEngine.Core.Controls;

using Vortice.Direct2D1;
using SkinEngine.Core.Rendering;

/// <summary>
/// Abstract base class for all UI controls in the bitmap-based UI engine.
/// Behavior belongs to the engine. Appearance belongs to the skin.
/// </summary>
public abstract class Control
{
    public string Id { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    public Rectangle Bounds => new(X, Y, Width, Height);

    public abstract void Draw(ID2D1DeviceContext5 ctx, SkinAtlas atlas);
    
    public virtual bool HitTest(int mx, int my) =>
        Visible && mx >= X && mx < X + Width && my >= Y && my < Y + Height;

    public virtual void OnMouseDown(int mx, int my) { }
    public virtual void OnMouseUp(int mx, int my) { }
    public virtual void OnMouseMove(int mx, int my) { }
    public virtual void OnMouseEnter() { }
    public virtual void OnMouseLeave() { }
}
