using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class SnapbarWindowSettingsStoreTests
{
    [Fact]
    public void LoadReturnsNullWhenTheSettingsFileDoesNotExist()
    {
        using var directory = new TemporaryDirectory();
        var store = new SnapbarWindowSettingsStore(Path.Combine(directory.Path, "snapbar-window.json"));

        Assert.Null(store.Load());
    }

    [Fact]
    public void SaveAndLoadRoundTripGeometry()
    {
        using var directory = new TemporaryDirectory();
        var store = new SnapbarWindowSettingsStore(Path.Combine(directory.Path, "snapbar-window.json"));
        var expected = new SnapbarWindowSettings(120, 32, 640, 96);

        store.Save(expected);

        Assert.Equal(expected, store.Load());
    }

    [Fact]
    public void LoadReturnsNullWhenTheSettingsFileContainsMalformedJson()
    {
        using var directory = new TemporaryDirectory();
        var settingsPath = Path.Combine(directory.Path, "snapbar-window.json");
        File.WriteAllText(settingsPath, "{");
        var store = new SnapbarWindowSettingsStore(settingsPath);

        Assert.Null(store.Load());
    }

    [Fact]
    public void SaveDoesNotThrowWhenTheSettingsParentIsAFile()
    {
        using var directory = new TemporaryDirectory();
        var parentPath = Path.Combine(directory.Path, "settings-parent");
        File.WriteAllText(parentPath, string.Empty);
        var store = new SnapbarWindowSettingsStore(Path.Combine(parentPath, "snapbar-window.json"));

        var exception = Record.Exception(() => store.Save(new SnapbarWindowSettings(120, 32, 640, 96)));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(double.NaN, 0, 400, 48)]
    [InlineData(0, 0, 0, 48)]
    [InlineData(0, 0, 400, -1)]
    public void IsFiniteAndPositiveRejectsInvalidGeometry(double left, double top, double width, double height)
    {
        Assert.False(new SnapbarWindowSettings(left, top, width, height).IsFiniteAndPositive());
    }

    [Fact]
    public void IsIntersectingRejectsAGeometryOutsideTheVisibleScreen()
    {
        var settings = new SnapbarWindowSettings(-500, 20, 400, 48);

        Assert.False(settings.IsIntersecting(0, 0, 1920, 1080));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"calendar-timeline-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
