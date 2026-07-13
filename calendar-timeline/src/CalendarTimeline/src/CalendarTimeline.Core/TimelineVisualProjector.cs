namespace CalendarTimeline.Core;

public static class TimelineVisualProjector
{
    public static IReadOnlyList<TimelineVisualBlock> Project(CalendarSnapshot snapshot)
    {
        var windowDuration = snapshot.WindowEnd - snapshot.WindowStart;
        var layout = TimelineLayout.Arrange(snapshot.Appointments);
        var blocks = new List<TimelineVisualBlock>(layout.Count);

        foreach (var block in layout)
        {
            var appointment = AppointmentSanitizer.Sanitize(block.Appointment);
            blocks.Add(new TimelineVisualBlock(
                appointment,
                block.Lane,
                CalculateRatio(snapshot.WindowStart, windowDuration, appointment.Start),
                CalculateRatio(snapshot.WindowStart, windowDuration, appointment.End),
                appointment.Start <= snapshot.GeneratedAt && appointment.End > snapshot.GeneratedAt,
                appointment.Title,
                CalendarTextFormatter.FormatTimeRange(appointment.Start, appointment.End).Split('–')[0],
                BuildSubtitle(appointment),
                appointment.CalendarColor,
                appointment.Categories.Select(category => category.Color).ToArray()));
        }

        return blocks;
    }

    private static double CalculateRatio(DateTimeOffset windowStart, TimeSpan windowDuration, DateTimeOffset point)
    {
        if (windowDuration <= TimeSpan.Zero)
        {
            return 0;
        }

        var ratio = (point - windowStart).TotalMilliseconds / windowDuration.TotalMilliseconds;
        return Math.Clamp(ratio, 0, 1);
    }

    private static string BuildSubtitle(Appointment appointment)
    {
        var parts = new List<string>
        {
            CalendarTextFormatter.FormatTimeRange(appointment.Start, appointment.End)
        };

        if (!string.IsNullOrWhiteSpace(appointment.Location))
        {
            parts.Add(appointment.Location);
        }

        if (!string.IsNullOrWhiteSpace(appointment.CalendarName))
        {
            parts.Add(appointment.CalendarName);
        }

        var categoryNames = appointment.Categories
            .Select(category => category.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name));
        var categories = string.Join(", ", categoryNames);
        if (!string.IsNullOrWhiteSpace(categories))
        {
            parts.Add(categories);
        }

        return string.Join(" · ", parts);
    }
}
