using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Details;

public sealed class RoleTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DETAIL-10")]
    public async Task TheSurfacesRoleAndTabIndexCanBeOverriddenAsRechartsAllowsAsync()
    {
        var example = await Reference.ExampleAsync("DetailsRoleRadialBarChart");
        var verdict = await Reference.CompareAsync("DetailsRoleRadialBarChart");
        var markup = Reference.Pictor(example);
        var plain = Reference.Pictor(example with { Spec = example.Spec with { Role = null, TabIndex = null } });

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Contains(" tabindex=\"2\" role=\"img\"", markup, StringComparison.Ordinal);
        Assert.Contains(" tabindex=\"0\" role=\"application\"", plain, StringComparison.Ordinal);
        var (set, unset) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example with { Spec = example.Spec with { Role = null, TabIndex = null } });
                var before = chart.Spec(500, 300);
                chart.Role = "img";
                chart.TabIndex = 2;
                return (chart.Spec(500, 300), before);
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(("img", 2), (set.Role, set.TabIndex));
        Assert.Equal(((string?)null, (int?)null), (unset.Role, unset.TabIndex));
    }
}
