namespace CalendarTimeline.Snapbar;

public sealed record SnapbarWindowSettings(double Left, double Top, double Width, double Height)
{
    public const double DefaultWidth = 400;

    public bool IsFiniteAndPositive()
    {
        return double.IsFinite(Left)
            && double.IsFinite(Top)
            && double.IsFinite(Width)
            && double.IsFinite(Height)
            && Width > 0
            && Height > 0;
    }

    public bool IsIntersecting(double screenLeft, double screenTop, double screenWidth, double screenHeight)
    {
        return IsFiniteAndPositive()
            && screenWidth > 0
            && screenHeight > 0
            && Left < screenLeft + screenWidth
            && Left + Width > screenLeft
            && Top < screenTop + screenHeight
            && Top + Height > screenTop;
    }
}
