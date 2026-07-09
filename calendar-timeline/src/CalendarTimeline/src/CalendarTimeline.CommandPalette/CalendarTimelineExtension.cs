using Microsoft.CommandPalette.Extensions;
using System.Runtime.InteropServices;
using System.Threading;

namespace CalendarTimeline.CommandPalette;

[Guid("8D8D0D07-371D-4F7A-984D-B52F4A09F399")]
public sealed partial class CalendarTimelineExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent extensionDisposedEvent;
    private readonly CalendarTimelineCommandsProvider provider = new();

    public CalendarTimelineExtension(ManualResetEvent extensionDisposedEvent)
    {
        this.extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType == ProviderType.Commands ? provider : null;
    }

    public void Dispose()
    {
        extensionDisposedEvent.Set();
    }
}
