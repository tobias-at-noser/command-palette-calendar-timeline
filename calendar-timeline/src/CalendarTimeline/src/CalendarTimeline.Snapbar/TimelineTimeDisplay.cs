using System.Globalization;

namespace CalendarTimeline.Snapbar;

public static class TimelineTimeDisplay
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string GetCurrentTime(DateTimeOffset now) =>
        now.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string GetDateTooltip(DateTimeOffset now)
    {
        return now.ToString("dddd, dd.MM.yyyy", GermanCulture);
    }

    public static TimelineCountdown? GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks)
    {
        var target = blocks
            .Where(block => block.End > block.Start && block.Start > now)
            .OrderBy(block => block.Start)
            .FirstOrDefault();
        if (target is null || target.Start - now <= TimeSpan.FromMinutes(5))
        {
            return null;
        }

        var minutes = (int)(Math.Round((target.Start - now).TotalMinutes / 5, MidpointRounding.AwayFromZero) * 5);
        return new TimelineCountdown($"{minutes / 60:D2}:{minutes % 60:D2}", target);
    }

    public static bool IsHighlighted(DateTimeOffset now, TimelineBlockViewModel block)
    {
        return block.End > block.Start
            && block.Start - TimeSpan.FromMinutes(5) <= now
            && now < block.End;
    }
}
