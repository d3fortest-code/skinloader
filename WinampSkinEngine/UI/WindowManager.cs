namespace WinampSkinEngine.UI;

using System.Drawing;

/// <summary>
/// Static tracker for all Winamp windows. Handles snapping windows together
/// when dragged close to each other.
/// </summary>
public static class WindowManager
{
    public const int SNAP_DISTANCE = 10;

    public static PlayerWindow?  Main     { get; set; }
    public static EqWindow?      Eq       { get; set; }
    public static PlaylistWindow? Playlist { get; set; }

    /// <summary>
    /// Call after the main window moves to reposition docked sub-windows.
    /// </summary>
    public static void OnMainMoved()
    {
        if (Main is null) return;

        int scale = Main.ScaleFactor;
        int baseH = 116 * scale;

        if (Eq?.Visible == true)
            SnapToMain(Eq, baseH);

        if (Playlist?.Visible == true)
            SnapToMain(Playlist, baseH + (Eq?.Visible == true ? baseH : 0));
    }

    /// <summary>
    /// Snap a window to the main window at the given vertical offset.
    /// </summary>
    public static void SnapToMain(Form window, int offsetY)
    {
        if (Main is null) return;
        window.Location = new Point(Main.Left, Main.Top + offsetY);
    }

    /// <summary>
    /// Check if two rectangles are within snap distance on any edge.
    /// Returns the snapped position for <paramref name="moving"/> if applicable.
    /// </summary>
    public static Point? TrySnap(Rectangle moving, Rectangle target)
    {
        int dx = 0, dy = 0;
        bool snapped = false;

        // Horizontal snap: left-to-right or right-to-left
        if (Math.Abs(moving.Left - target.Right) <= SNAP_DISTANCE)
        { dx = target.Right - moving.Left; snapped = true; }
        else if (Math.Abs(moving.Right - target.Left) <= SNAP_DISTANCE)
        { dx = target.Left - moving.Right; snapped = true; }

        // Vertical snap: bottom-to-top or top-to-bottom
        if (Math.Abs(moving.Bottom - target.Top) <= SNAP_DISTANCE)
        { dy = target.Top - moving.Bottom; snapped = true; }
        else if (Math.Abs(moving.Top - target.Bottom) <= SNAP_DISTANCE)
        { dy = target.Bottom - moving.Top; snapped = true; }

        return snapped ? new Point(moving.X + dx, moving.Y + dy) : null;
    }
}
