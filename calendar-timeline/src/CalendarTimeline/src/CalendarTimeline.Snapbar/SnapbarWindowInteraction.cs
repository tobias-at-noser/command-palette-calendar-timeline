namespace CalendarTimeline.Snapbar;

public enum SnapbarResizeDirection
{
    None,
    Left,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft
}

public static class SnapbarWindowInteraction
{
    public const double DefaultResizeBorder = 18;
    private const int SystemCommandMask = 0xFFF0;
    private const int SystemCommandMinimize = 0xF020;
    private const int SystemCommandMaximize = 0xF030;

    public static SnapbarResizeDirection GetResizeDirection(
        double x,
        double y,
        double width,
        double height,
        double resizeBorder)
    {
        var left = x <= resizeBorder;
        var right = x >= width - resizeBorder;
        var top = y <= resizeBorder;
        var bottom = y >= height - resizeBorder;

        if (top && left) return SnapbarResizeDirection.TopLeft;
        if (top && right) return SnapbarResizeDirection.TopRight;
        if (bottom && right) return SnapbarResizeDirection.BottomRight;
        if (bottom && left) return SnapbarResizeDirection.BottomLeft;
        if (left) return SnapbarResizeDirection.Left;
        if (top) return SnapbarResizeDirection.Top;
        if (right) return SnapbarResizeDirection.Right;
        if (bottom) return SnapbarResizeDirection.Bottom;

        return SnapbarResizeDirection.None;
    }

    public static bool CanBeginDrag(bool isAppointmentTarget) => !isAppointmentTarget;

    public static bool IsWithinBounds(int x, int y, int left, int top, int right, int bottom)
    {
        return x >= left && x < right && y >= top && y < bottom;
    }

    public static bool ShouldBlockSystemCommand(int command)
    {
        return (command & SystemCommandMask) is SystemCommandMinimize or SystemCommandMaximize;
    }
}
