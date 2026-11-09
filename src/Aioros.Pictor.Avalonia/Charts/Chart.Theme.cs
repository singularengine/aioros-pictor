namespace Aioros.Pictor;

/// <summary>The chart's theme and its elements' place in the logical tree.</summary>
public abstract partial class Chart
{
    private readonly ChartScope _scope;

    /// <summary>Draws the chart again in its theme's colors and font, after its theme variant or a resource changed.</summary>
    internal void Retheme()
    {
        _paints = null;
        Invalidate();
    }
}
