using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using System.Threading;

namespace CalendarTimeline.CommandPalette;

public static class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "-RegisterProcessAsComServer")
        {
            Console.WriteLine("Not being launched as a Command Palette extension.");
            return;
        }

        using var extensionDisposedEvent = new ManualResetEvent(false);
        var server = new ComServer();
        var extension = new CalendarTimelineExtension(extensionDisposedEvent);

        server.RegisterClass<CalendarTimelineExtension, IExtension>(() => extension);
        server.Start();
        extensionDisposedEvent.WaitOne();
        server.Stop();
        server.UnsafeDispose();
    }
}
