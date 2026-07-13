using CalendarTimeline.Host;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class HostSettingsStoreTests
{
    [Fact]
    public void HostSettingsDefaultUsesExpectedValues()
    {
        var settings = HostSettings.Default;

        Assert.False(settings.AutostartEnabled);
        Assert.True(settings.ShowSnapbar);
        Assert.Equal("Top", settings.Edge);
    }

    [Fact]
    public async Task LoadAsyncReturnsDefaultsWhenFileMissing()
    {
        using var directory = new TemporaryDirectory();
        var store = new HostSettingsStore(Path.Combine(directory.Path, "settings.json"));

        var settings = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(HostSettings.Default, settings);
    }

    [Fact]
    public async Task SaveAsyncRoundTripsSettings()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        var store = new HostSettingsStore(path);
        var expected = new HostSettings(true, false, "Bottom");

        await store.SaveAsync(expected, CancellationToken.None);
        var actual = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task LoadSynchronouslyReadsStoredSettings()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        var store = new HostSettingsStore(path);
        var expected = new HostSettings(true, false, "Bottom");

        await store.SaveAsync(expected, CancellationToken.None);

        Assert.Equal(expected, store.Load());
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
