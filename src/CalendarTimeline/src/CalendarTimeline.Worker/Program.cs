using System.Text.Json;
using CalendarTimeline.Worker;

var options = new JsonSerializerOptions
{
    WriteIndented = true
};

if (args.Contains("--fake-once", StringComparer.OrdinalIgnoreCase))
{
    var snapshot = FakeSnapshotFactory.Create(DateTimeOffset.Now);
    Console.WriteLine(JsonSerializer.Serialize(snapshot, options));
    return 0;
}

Console.Error.WriteLine("Use --fake-once to emit one calendar snapshot.");
return 1;
