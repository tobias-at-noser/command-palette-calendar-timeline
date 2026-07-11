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
    public void OutlookSourceUsesOutlookComApplicationOnWindows()
    {
        var source = File.ReadAllText(WorkerFile("OutlookCalendarSnapshotSource.cs"));

        Assert.Contains("Type.GetTypeFromProgID(\"Outlook.Application\")", source);
        Assert.Contains("GetDefaultFolder", source);
        Assert.DoesNotContain("Outlook COM calendar loading is not implemented yet", source);
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
}
