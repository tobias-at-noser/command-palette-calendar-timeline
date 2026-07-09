using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public sealed class CalendarTimelineCommandsProvider
{
    private readonly CalendarTimelineDockBand dockBand = new();

    public CalendarTimelineDockBand DockBand => dockBand;

    public void Update(CalendarSnapshot snapshot)
    {
        dockBand.Update(snapshot);
    }

    public bool TryApplyWorkerSnapshot(string json)
    {
        try
        {
            dockBand.Update(CalendarSnapshotJson.Deserialize(json));
            return true;
        }
        catch
        {
            dockBand.ApplyWorkerError("Outlook-Kalender nicht verfügbar");
            return false;
        }
    }
}
