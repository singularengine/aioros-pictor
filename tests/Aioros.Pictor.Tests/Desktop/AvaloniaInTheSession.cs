using System.Reflection;
using Avalonia.Threading;
using Xunit.v3;

[assembly: Aioros.Pictor.Tests.Desktop.AvaloniaInTheSession]

namespace Aioros.Pictor.Tests.Desktop;

/// <summary>
/// Fails a test that made an Avalonia object on its own thread instead of inside <see cref="DesktopSession"/>'s dispatch.
/// The session recreates the UI dispatcher for every dispatch, and the first thread that touches Avalonia meanwhile
/// becomes the UI thread, so such a test makes another test's dispatch throw at random.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AvaloniaInTheSessionAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (Dispatcher.FromThread(Thread.CurrentThread) is not null)
        {
            throw new InvalidOperationException($"{test.TestDisplayName} made an Avalonia object outside DesktopSession.Session.Dispatch.");
        }
    }
}
