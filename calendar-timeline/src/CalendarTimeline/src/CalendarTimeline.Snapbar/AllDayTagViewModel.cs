namespace CalendarTimeline.Snapbar;

public sealed class AllDayTagViewModel
{
    public AllDayTagViewModel(
        string title,
        int additionalCount,
        IReadOnlyList<string> tooltipTitles,
        string calendarIdentity,
        string? calendarColor,
        IReadOnlyList<string?> categoryColors,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        Title = title;
        AdditionalCount = additionalCount;
        TooltipTitles = tooltipTitles;
        CalendarIdentity = calendarIdentity;
        CalendarColor = calendarColor;
        CategoryColors = categoryColors;
        Start = start;
        End = end;
    }

    public string Title { get; }

    public int AdditionalCount { get; }

    public IReadOnlyList<string> TooltipTitles { get; }

    public string CalendarIdentity { get; }

    public string? CalendarColor { get; }

    public IReadOnlyList<string?> CategoryColors { get; }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }
}
