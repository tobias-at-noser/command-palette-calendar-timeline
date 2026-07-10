namespace CalendarTimeline.Core;

public static class DockAgendaProjector
{
    public static IReadOnlyList<DockAgendaItem> Project(CalendarSnapshot snapshot, int maxItems)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        var arranged = TimelineLayout.Arrange(snapshot.Appointments)
            .ToDictionary(block => block.Appointment.Id);
        var running = snapshot.Appointments
            .Where(appointment => appointment.Start <= snapshot.GeneratedAt && appointment.End > snapshot.GeneratedAt)
            .OrderBy(appointment => appointment.Start)
            .ThenBy(appointment => appointment.End)
            .ToList();
        var upcoming = snapshot.Appointments
            .Where(appointment => appointment.Start > snapshot.GeneratedAt)
            .OrderBy(appointment => appointment.Start)
            .ThenBy(appointment => appointment.End)
            .ToList();
        var items = new List<DockAgendaItem>(maxItems);

        foreach (var appointment in running)
        {
            if (items.Count == maxItems)
            {
                break;
            }

            items.Add(CreateRunningItem(arranged[appointment.Id].Lane, appointment, snapshot.GeneratedAt));
        }

        var nextUpcomingPrefixed = false;

        foreach (var appointment in upcoming)
        {
            if (items.Count == maxItems)
            {
                break;
            }

            items.Add(CreateUpcomingItem(arranged[appointment.Id].Lane, appointment, !nextUpcomingPrefixed));
            nextUpcomingPrefixed = true;
        }

        if (items.Count > 0)
        {
            return items;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.StatusMessage))
        {
            return [new DockAgendaItem(snapshot.StatusMessage!, string.Empty, 0, false, null, null)];
        }

        return [];
    }

    private static DockAgendaItem CreateRunningItem(int lane, Appointment appointment, DateTimeOffset now)
    {
        var sanitized = AppointmentSanitizer.Sanitize(appointment);
        return new DockAgendaItem(
            $"Jetzt · {sanitized.Title}",
            BuildRunningSubtitle(sanitized, now),
            lane,
            true,
            sanitized.TeamsUrl,
            sanitized.Id);
    }

    private static DockAgendaItem CreateUpcomingItem(int lane, Appointment appointment, bool prefixAsNext)
    {
        var sanitized = AppointmentSanitizer.Sanitize(appointment);
        return new DockAgendaItem(
            prefixAsNext ? $"Als Nächstes · {sanitized.Title}" : sanitized.Title,
            BuildUpcomingSubtitle(sanitized),
            lane,
            false,
            sanitized.TeamsUrl,
            sanitized.Id);
    }

    private static string BuildRunningSubtitle(Appointment appointment, DateTimeOffset now)
    {
        var parts = new List<string>
        {
            $"{CalendarTextFormatter.FormatDuration(appointment.End - now)} verbleibend"
        };

        if (!string.IsNullOrWhiteSpace(appointment.Location))
        {
            parts.Add(appointment.Location);
        }

        return string.Join(" · ", parts);
    }

    private static string BuildUpcomingSubtitle(Appointment appointment)
    {
        var parts = new List<string>
        {
            CalendarTextFormatter.FormatTimeRange(appointment.Start, appointment.End)
        };

        if (!string.IsNullOrWhiteSpace(appointment.Location))
        {
            parts.Add(appointment.Location);
        }

        return string.Join(" · ", parts);
    }
}
