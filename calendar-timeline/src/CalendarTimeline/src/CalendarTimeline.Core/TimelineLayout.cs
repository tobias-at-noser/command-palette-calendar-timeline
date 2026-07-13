namespace CalendarTimeline.Core;

public static class TimelineLayout
{
    public static IReadOnlyList<TimelineBlock> Arrange(IReadOnlyList<Appointment> appointments)
    {
        var ordered = appointments
            .OrderBy(appointment => appointment.Start)
            .ThenBy(appointment => appointment.End)
            .ThenBy(appointment => appointment.Id, StringComparer.Ordinal)
            .ToList();
        var laneEnds = new List<DateTimeOffset>();
        var blocks = new List<TimelineBlock>();

        foreach (var appointment in ordered)
        {
            var lane = FindLane(laneEnds, appointment.Start);

            if (lane == laneEnds.Count)
            {
                laneEnds.Add(appointment.End);
            }
            else
            {
                laneEnds[lane] = appointment.End;
            }

            blocks.Add(new TimelineBlock(appointment, lane));
        }

        return blocks;
    }

    private static int FindLane(IReadOnlyList<DateTimeOffset> laneEnds, DateTimeOffset start)
    {
        for (var index = 0; index < laneEnds.Count; index++)
        {
            if (laneEnds[index] <= start)
            {
                return index;
            }
        }

        return laneEnds.Count;
    }
}
