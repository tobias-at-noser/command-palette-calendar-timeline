using CalendarTimeline.Core;
using CalendarTimeline.Worker;

if (args.Contains("--fake-once", StringComparer.OrdinalIgnoreCase))
{
    var snapshot = FakeSnapshotFactory.Create(DateTimeOffset.Now);
    Console.WriteLine(CalendarSnapshotJson.Serialize(snapshot));
    return 0;
}

Console.Error.WriteLine("Use --fake-once to emit one calendar snapshot.");
return 1;
