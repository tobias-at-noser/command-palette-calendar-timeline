using System.Xml.Linq;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CommandPalettePackageMetadataTests
{
    [Fact]
    public void CommandPaletteProjectIsConfiguredForMsixSideloading()
    {
        var project = XDocument.Load(ProjectFile("CalendarTimeline.CommandPalette.csproj"));
        var properties = project.Root!.Element("PropertyGroup")!;

        var windowsOutputType = project.Descendants("OutputType")
            .Single(element => element.Attribute("Condition")?.Value == "$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))")
            .Value;
        Assert.Equal("WinExe", windowsOutputType);
        Assert.Equal("app.manifest", properties.Element("ApplicationManifest")?.Value);
        var targetFrameworkValues = project.Descendants("TargetFrameworks")
            .Select(element => element.Value)
            .ToArray();
        Assert.Contains("net10.0;net10.0-windows10.0.26100.0", targetFrameworkValues);
        Assert.Equal("10.0.19041.0", properties.Element("TargetPlatformMinVersion")?.Value);
        Assert.Equal("true", properties.Element("EnableWindowsTargeting")?.Value);
        Assert.Equal("true", properties.Element("EnableMsixTooling")?.Value);
        Assert.Equal("win-x64;win-arm64", properties.Element("RuntimeIdentifiers")?.Value);
        Assert.True(File.Exists(ProjectFile(Path.Combine("Properties", "PublishProfiles", "win-x64.pubxml"))));
        Assert.True(File.Exists(ProjectFile(Path.Combine("Properties", "PublishProfiles", "win-arm64.pubxml"))));

        var releaseProperties = project.Descendants("PropertyGroup")
            .Single(element => element.Attribute("Condition")?.Value == "'$(Configuration)'!='Debug'");
        Assert.Equal("false", releaseProperties.Element("PublishTrimmed")?.Value);

        var windowsUseAppHost = project.Descendants("UseAppHost")
            .Single(element => element.Attribute("Condition")?.Value == "$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))")
            .Value;
        var nonWindowsUseAppHost = project.Descendants("UseAppHost")
            .Single(element => element.Attribute("Condition")?.Value == "!$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))")
            .Value;
        Assert.Equal("true", windowsUseAppHost);
        Assert.Equal("false", nonWindowsUseAppHost);

        var packageReferences = project.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .ToHashSet();

        Assert.Contains("Microsoft.CommandPalette.Extensions", packageReferences);
        Assert.Contains("Microsoft.Windows.CsWinRT", packageReferences);
        Assert.Contains("Shmuelie.WinRTServer", packageReferences);
        Assert.Contains("Microsoft.Windows.SDK.BuildTools.MSIX", packageReferences);
    }

    [Fact]
    public void SideloadManifestUsesRequiredAppxManifestFileNameWithoutPublishCopyTarget()
    {
        var project = XDocument.Load(ProjectFile("CalendarTimeline.CommandPalette.csproj"));

        var manifestPath = ProjectFile("AppxManifest.xml");

        Assert.True(File.Exists(manifestPath));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath)!, "Package.appxmanifest")));
        Assert.DoesNotContain(
            project.Descendants("Target"),
            element => element.Attribute("Name")?.Value == "CopyLooseAppxManifestForSideloadRegistration");
    }

    [Fact]
    public void AppxManifestReferencesExistingVisualAssetFiles()
    {
        XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        var document = XDocument.Load(ProjectFile("AppxManifest.xml"));
        var visualElements = document.Descendants(uap + "VisualElements").Single();
        var defaultTile = visualElements.Element(uap + "DefaultTile")!;
        var splashScreen = visualElements.Element(uap + "SplashScreen")!;
        var assetPaths = new[]
        {
            visualElements.Attribute("Square150x150Logo")?.Value,
            visualElements.Attribute("Square44x44Logo")?.Value,
            defaultTile.Attribute("Wide310x150Logo")?.Value,
            splashScreen.Attribute("Image")?.Value,
        };

        foreach (var assetPath in assetPaths)
        {
            Assert.False(string.IsNullOrWhiteSpace(assetPath));
            Assert.True(File.Exists(ProjectFile(assetPath!)), assetPath);
        }
    }

    [Fact]
    public void AppxManifestUsesValidResourceLanguageForLooseRegistration()
    {
        XNamespace manifest = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        var document = XDocument.Load(ProjectFile("AppxManifest.xml"));
        var language = document.Descendants(manifest + "Resource").Single().Attribute("Language")?.Value;

        Assert.Equal("en-US", language);
        Assert.NotEqual("x-generate", language);
    }

    [Fact]
    public void ReadmeDocumentsRegisteringPublishedAppxManifest()
    {
        var readme = File.ReadAllText(ProjectFile(Path.Combine("..", "..", "..", "README.md")));

        Assert.Contains("Add-AppxPackage -Register .\\AppxManifest.xml", readme);
        Assert.DoesNotContain("Add-AppxPackage -Register .\\Package.appxmanifest", readme);
    }

    [Fact]
    public void CommandPaletteWinmdReferenceDeclaresNativeImplementation()
    {
        var project = XDocument.Load(ProjectFile("CalendarTimeline.CommandPalette.csproj"));
        var winmdReference = project.Descendants("WindowsMetadataReference")
            .Single(element =>
                element.Attribute("Include")?.Value.Contains("Microsoft.CommandPalette.Extensions.winmd") == true
                || element.Attribute("Update")?.Value.Contains("Microsoft.CommandPalette.Extensions.winmd") == true);

        Assert.Equal("Microsoft.CommandPalette.Extensions.dll", winmdReference.Attribute("Implementation")?.Value);
    }

    [Fact]
    public void WindowsDockBandOverridesSdkSubtitle()
    {
        var source = File.ReadAllText(ProjectFile("CalendarTimelineDockBand.cs"));

        Assert.Contains("public override string Subtitle", source);
        Assert.Contains("private string subtitle", source);
    }

    [Fact]
    public void ProgramUsesCsWinRtRegisterClassOverload()
    {
        var source = File.ReadAllText(ProjectFile("Program.cs"));

        Assert.Contains("using Shmuelie.WinRTServer.CsWinRT;", source);
        Assert.Contains("server.RegisterClass<CalendarTimelineExtension, IExtension>(() => extension);", source);
    }

    [Fact]
    public void PackageManifestRegistersCalendarTimelineCommandPaletteExtension()
    {
        XNamespace manifest = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace uap3 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/3";
        XNamespace com = "http://schemas.microsoft.com/appx/manifest/com/windows10";
        var document = XDocument.Load(ProjectFile("AppxManifest.xml"));

        var identity = document.Root!.Element(manifest + "Identity")!;
        Assert.Equal("CalendarTimeline.CommandPalette", identity.Attribute("Name")?.Value);
        Assert.Equal("0.1.0.0", identity.Attribute("Version")?.Value);

        var appExtension = document.Descendants(uap3 + "AppExtension").Single();
        Assert.Equal("com.microsoft.commandpalette", appExtension.Attribute("Name")?.Value);
        Assert.Equal("CalendarTimeline", appExtension.Attribute("Id")?.Value);
        Assert.Equal("Calendar Timeline", appExtension.Attribute("DisplayName")?.Value);

        var createInstance = appExtension.Descendants(manifest + "CreateInstance").Single();
        var classId = createInstance.Attribute("ClassId")?.Value;
        Assert.False(string.IsNullOrWhiteSpace(classId));
        Assert.NotEqual("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", classId);

        var comClass = document.Descendants(com + "Class").Single();
        Assert.Equal(classId, comClass.Attribute("Id")?.Value);
    }

    private static string ProjectFile(string fileName)
    {
        var normalizedFileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "CalendarTimeline.CommandPalette",
                normalizedFileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }
}
