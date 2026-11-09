using Aioros.Pictor.Demo;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class AutomationDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "A11Y-01")]
    public async Task AChartReadsItsNameAndEachCategorysDataAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (name, type, help, items, changed) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Title = "Pages";
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var peer = ControlAutomationPeer.CreatePeerForElement(chart);
                var children = peer.GetChildren().Select(child => child.GetName()).ToList();
                var described = peer.GetHelpText();
                chart.Data = Reference.Rows(example).Take(3).ToList();
                var after = peer.GetChildren().Count;
                var result = (peer.GetName(), peer.GetAutomationControlType(), described, children, after);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Pages", name);
        Assert.Equal(AutomationControlType.List, type);
        Assert.Equal("LineChart, 7 items", help);
        Assert.Equal(7, items.Count);
        Assert.Equal("Page A, pv: 2400, uv: 4000", items[0]);
        Assert.Equal("Page G, pv: 4300, uv: 3490", items[6]);
        Assert.Equal(3, changed);
    }

    [Fact]
    [Trait("AC", "A11Y-02")]
    public async Task AnItemChartReadsEachItemAndTakesTheAppsNamesAsync()
    {
        var example = await Reference.ExampleAsync("TwoSimplePieChart");

        var (name, help, items, focusable) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                AutomationProperties.SetName(chart, "Groups");
                AutomationProperties.SetHelpText(chart, "Two pies of groups");
                chart.AccessibilityLayer = true;
                var window = new Window { Width = 800, Height = 400, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var peer = ControlAutomationPeer.CreatePeerForElement(chart);
                var result = (peer.GetName(), peer.GetHelpText(), peer.GetChildren().Select(child => child.GetName()).ToList(), peer.IsKeyboardFocusable());
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Groups", name);
        Assert.Equal("Two pies of groups", help);
        Assert.Equal(12, items.Count);
        Assert.Equal("Group A: 400", items[0]);
        Assert.True(focusable);
    }
}
