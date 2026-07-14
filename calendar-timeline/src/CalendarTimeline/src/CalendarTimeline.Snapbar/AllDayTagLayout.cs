namespace CalendarTimeline.Snapbar;

public static class AllDayTagLayout
{
    public const double TagHeight = 16;
    public const double TagWidth = 144;
    public const double GapFromNowLine = 8;

    public static (double Left, double Width) GetBounds(
        double timelineWidth,
        double nowRatio,
        double startRatio,
        double endRatio)
    {
        var nowX = timelineWidth * nowRatio;
        var startX = timelineWidth * startRatio;
        var endX = timelineWidth * endRatio;
        var parkedLeft = nowX + GapFromNowLine;
        var parkedRight = parkedLeft + TagWidth;

        if (startX > nowX)
        {
            return (startX, TagWidth);
        }

        if (endX >= parkedRight)
        {
            return (parkedLeft, TagWidth);
        }

        return (endX - TagWidth, TagWidth);
    }
}
