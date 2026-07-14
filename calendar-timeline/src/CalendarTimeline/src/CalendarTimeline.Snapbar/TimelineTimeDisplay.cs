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

    public static string? GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks)
    {
        var materializedBlocks = blocks.Where(block => block.End > block.Start).ToArray();
        if (materializedBlocks.Any(block => block.Start <= now && now < block.End))
        {
            return null;
        }

        var nextBlock = materializedBlocks
            .Where(block => block.Start > now)
            .OrderBy(block => block.Start)
            .FirstOrDefault();
        if (nextBlock is null)
        {
            return null;
        }

        var roundedMinutes = (int)(Math.Round(
            (nextBlock.Start - now).TotalMinutes / 5,
            MidpointRounding.AwayFromZero) * 5);
        var duration = TimeSpan.FromMinutes(roundedMinutes);
        return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";
    }
}
