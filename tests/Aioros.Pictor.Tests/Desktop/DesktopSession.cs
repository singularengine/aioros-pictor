using Aioros.Pictor.Demo;
using Avalonia.Headless;

[assembly: AssemblyFixture(typeof(Aioros.Pictor.Tests.Desktop.DesktopSession))]

namespace Aioros.Pictor.Tests.Desktop;

public sealed class DesktopSession : IAsyncLifetime
{
    public HeadlessUnitTestSession Session { get; } = DesktopRender.Start();

    public async ValueTask InitializeAsync() => await Session.Dispatch(static () => 0, CancellationToken.None);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
