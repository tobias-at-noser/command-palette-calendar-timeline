namespace CalendarTimeline.Core;

public static class CalendarTextFormatter
{
    public static string FormatTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        return $"{start:HH:mm}–{end:HH:mm}";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        var minutes = Math.Max(0, (int)Math.Ceiling(duration.TotalMinutes));

        if (minutes < 60)
        {
            return $"{minutes} Min.";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        return remainingMinutes == 0 ? $"{hours} Std." : $"{hours} Std. {remainingMinutes} Min.";
    }
}
