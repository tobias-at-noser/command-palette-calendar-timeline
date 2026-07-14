namespace CalendarTimeline.Core;

public sealed record Appointment
{
    private IReadOnlyList<CalendarCategory> categories = [];

    public Appointment(
        string Id,
        string Title,
        string Location,
        DateTimeOffset Start,
        DateTimeOffset End,
        bool IsPrivate,
        bool IsConfidential,
        string? TeamsUrl,
        string CalendarId = "",
        string CalendarName = "",
        string? CalendarColor = null,
        IReadOnlyList<CalendarCategory>? Categories = null,
        bool IsAllDayEvent = false)
    {
        this.Id = Id;
        this.Title = Title;
        this.Location = Location;
        this.Start = Start;
        this.End = End;
        this.IsPrivate = IsPrivate;
        this.IsConfidential = IsConfidential;
        this.TeamsUrl = TeamsUrl;
        this.CalendarId = CalendarId;
        this.CalendarName = CalendarName;
        this.CalendarColor = CalendarColor;
        this.Categories = Categories ?? [];
        this.IsAllDayEvent = IsAllDayEvent;
    }

    public string Id { get; init; }

    public string Title { get; init; }

    public string Location { get; init; }

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    public bool IsPrivate { get; init; }

    public bool IsConfidential { get; init; }

    public string? TeamsUrl { get; init; }

    public string CalendarId { get; init; }

    public string CalendarName { get; init; }

    public string? CalendarColor { get; init; }

    public IReadOnlyList<CalendarCategory> Categories
    {
        get => categories;
        init => categories = value ?? [];
    }

    public bool IsAllDayEvent { get; init; }
}
