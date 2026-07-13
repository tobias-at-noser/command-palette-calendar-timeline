using System.Xml.Linq;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class OutlookCalendarSnapshotSourceTests
{
    [Fact]
    public void WorkerProjectBuildsWindowsTargetForOutlookComSource()
    {
        var project = XDocument.Load(WorkerFile("CalendarTimeline.Worker.csproj"));
        var targetFrameworkValues = project.Descendants("TargetFrameworks")
            .Select(element => element.Value)
            .ToArray();
        var windowsUseAppHost = project.Descendants("UseAppHost")
            .Single(element => element.Attribute("Condition")?.Value == "$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))")
            .Value;
        var nonWindowsUseAppHost = project.Descendants("UseAppHost")
            .Single(element => element.Attribute("Condition")?.Value == "!$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))")
            .Value;

        Assert.Contains("net10.0;net10.0-windows10.0.19041.0", targetFrameworkValues);
        Assert.Equal("true", project.Root!.Element("PropertyGroup")!.Element("EnableWindowsTargeting")?.Value);
        Assert.Equal("true", windowsUseAppHost);
        Assert.Equal("false", nonWindowsUseAppHost);
    }

    [Fact]
    public void HostProjectUsesWindowsWorkerTargetForWindowsHost()
    {
        var project = XDocument.Load(HostFile("CalendarTimeline.Host.csproj"));
        var workerTargetFrameworks = project.Descendants("WorkerTargetFramework").ToArray();

        Assert.Contains(workerTargetFrameworks, element =>
            element.Attribute("Condition")?.Value.Contains("-windows", StringComparison.Ordinal) == true
            && element.Value == "net10.0-windows10.0.19041.0");
    }

    [Fact]
    public void HostProjectPrefersWindowsTargetForDefaultWindowsRun()
    {
        var project = XDocument.Load(HostFile("CalendarTimeline.Host.csproj"));
        var targetFrameworks = project.Descendants("TargetFrameworks")
            .Single(element => element.Attribute("Condition")?.Value.StartsWith("'$(OS)' == 'Windows_NT'", StringComparison.Ordinal) == true)
            .Value;

        Assert.Equal("net10.0-windows10.0.19041.0;net10.0", targetFrameworks);
    }

    [Fact]
    public void OutlookSourceUsesOutlookComApplicationOnWindows()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("Type.GetTypeFromProgID(\"Outlook.Application\")", source);
        Assert.Contains("DefaultStore", source);
        Assert.DoesNotContain("Outlook COM calendar loading is not implemented yet", source);
    }

    [Fact]
    public void OutlookSourceReleasesCollectedCalendarFoldersInOneEnclosingFinally()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("Marshal.FinalReleaseComObject", source);
        Assert.Contains("ReleaseComObject(restrictedItems)", source);
        Assert.Contains("ReleaseComObject(item)", source);
        Assert.Contains("ReleaseComObject(categories)", source);
        Assert.Contains(
            "finally\n            {\n                foreach (var calendarFolder in calendarFolders)\n                {\n                    ReleaseComObject(calendarFolder);\n                }",
            source);
        Assert.Equal(1, CountOccurrences(source, "ReleaseComObject(calendarFolder);"));
        Assert.Contains("hadCalendarLoadFailures = true", source);
        Assert.DoesNotContain("ref bool hadFailures", source);
        Assert.DoesNotContain("defaultCalendarFolder", source);
        Assert.DoesNotContain("GetDefaultFolder", source);
    }

    [Fact]
    public void OutlookSourceSkipsUnreadableRestrictedItemsWithoutFailingTheirCalendar()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("var calendarColor = TryGetCalendarColor(calendarFolder);", source);
        Assert.DoesNotContain("GetStableCalendarColor", source);
        Assert.Contains("catch (OperationCanceledException)\n                {\n                    throw;\n                }", source);
        Assert.Contains(
            "// A malformed or inaccessible item must not make its calendar unreadable.",
            source);
        Assert.Contains("ReleaseComObject(item);", source);
    }

    [Fact]
    public void OutlookSourceIteratesRecurringRestrictedItemsWithoutUsingUndefinedCount()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.DoesNotContain("((dynamic)restrictedItems).Count", source);
        Assert.Contains("((dynamic)restrictedItems).GetFirst()", source);
        Assert.Contains("((dynamic)restrictedItems).GetNext()", source);
    }

    [Fact]
    public void OutlookSourceReleasesCurrentRecurringItemWhenCancellationIsRequested()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains(
            "object? item = null;\n\n            try\n            {\n                item = ((dynamic)restrictedItems).GetFirst();",
            source);
        Assert.Contains(
            "finally\n            {\n                ReleaseComObject(item);\n            }\n\n            return appointments;",
            source);
    }

    [Fact]
    public void OutlookSourceTransfersFolderOwnershipOnlyAfterCalendarIdentification()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("rootFolderTransferred = CollectCalendarFolders", source);
        Assert.Contains(
            "if (!rootFolderTransferred && !IsRetainedCalendarFolder(rootFolder, calendarFolders))",
            source);
        Assert.Contains("private static bool CollectCalendarFolders", source);
        Assert.Contains("isCalendar = Convert.ToInt32(folder.DefaultItemType) == 1", source);
        Assert.Contains("return isCalendar;", source);
    }

    [Fact]
    public void OutlookSourceRethrowsDiscoveryCancellationAfterReleasingUnretainedChildren()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains(
            "catch (OperationCanceledException)\n                {\n                    throw;\n                }\n                catch\n                {\n                    // An unreadable child does not prevent discovery of later folders.\n                }\n                finally\n                {\n                    if (!IsRetainedCalendarFolder(child, calendarFolders))\n                    {\n                        ReleaseComObject(child);\n                    }\n                }",
            source);
        Assert.Contains("private static bool IsRetainedCalendarFolder", source);
    }

    [Fact]
    public void OutlookSourcePropagatesCancellationThroughSynchronousComLoading()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("var result = LoadAppointments(now, cancellationToken);", source);
        Assert.Contains("private static CalendarLoadResult LoadAppointments(DateTimeOffset now, CancellationToken cancellationToken)", source);
        Assert.Contains("CollectCalendarFolders(rootFolder, defaultStoreId, calendarFolders, cancellationToken)", source);
        Assert.Contains("LoadCalendarAppointments(calendarFolder, outlookNamespace, now, cancellationToken)", source);
        Assert.Contains("List<object> calendarFolders,\n        CancellationToken cancellationToken)", source);
        Assert.Contains("object outlookNamespace,\n        DateTimeOffset now,\n        CancellationToken cancellationToken)", source);
    }

    [Fact]
    public void OutlookSourceChecksCancellationBeforeFolderAndAppointmentComItems()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains(
            "for (var index = 1; index <= count; index++)\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                object? child = null;",
            source);
        Assert.Contains(
            "foreach (var calendarFolder in calendarFolders)\n                {\n                    cancellationToken.ThrowIfCancellationRequested();\n                    try",
            source);
        Assert.Contains(
            "while (item is not null)\n                {\n                    cancellationToken.ThrowIfCancellationRequested();\n                    try",
            source);
        Assert.True(
            CountOccurrences(source, "cancellationToken.ThrowIfCancellationRequested();") >= 5,
            "Expected cancellation checks at COM-loading boundaries and inside folder and appointment loops.");
    }

    private static string WorkerFile(string fileName)
    {
        return ProjectFile("CalendarTimeline.Worker", fileName);
    }

    private static string HostFile(string fileName)
    {
        return ProjectFile("CalendarTimeline.Host", fileName);
    }

    private static string ProjectFile(string projectName, string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                projectName,
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var startIndex = 0;

        while ((startIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
    }
}
