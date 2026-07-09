namespace CalendarTimeline.Core;

public sealed record CalendarWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public static CalendarWindow Create(DateTimeOffset now)
    {
        return new CalendarWindow(now.AddMinutes(-30), now.AddHours(4));
    }
}
