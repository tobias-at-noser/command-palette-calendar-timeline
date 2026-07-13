namespace CalendarTimeline.Host;

public sealed record HostSettings(bool AutostartEnabled, bool ShowSnapbar, string Edge)
{
    public static HostSettings Default { get; } = new(false, true, "Top");
}
