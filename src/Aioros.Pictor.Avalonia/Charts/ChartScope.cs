using System.Collections;
using Aioros.Pictor.Drawing;
using Aioros.Pictor.Theming;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Aioros.Pictor;

/// <summary>
/// What places a chart in Avalonia's trees: its theme, resolved from its resources once and resolved again when its
/// theme variant or a resource in its scope changes, and its elements as its logical children, so that styles, control
/// themes, resources and the data context reach them.
/// </summary>
internal sealed class ChartScope
{
    private readonly Chart _chart;
    private readonly IList<ILogical> _logical;
    private ChartTheme? _colors;

    public ChartScope(Chart chart, IList<ILogical> logical, Avalonia.Collections.AvaloniaList<IChartElement> elements)
    {
        _chart = chart;
        _logical = logical;
        elements.CollectionChanged += (_, args) => Adopt(args.OldItems, args.NewItems);
        chart.ActualThemeVariantChanged += (_, _) => Changed();
        chart.ResourcesChanged += (_, _) => Changed();
    }

    /// <summary>
    /// The theme the chart draws with, resolved from its resources once and kept until its theme variant or a resource in
    /// its scope changes (<see cref="PictorKeys.Resolve"/>).
    /// </summary>
    public ChartTheme Colors => _colors ??= PictorKeys.Resolve(_chart);

    /// <summary>The chart's templates of custom content, compiled once per theme.</summary>
    public ContentTemplates Templates { get; } = new();

    /// <summary>A builder of the chart's spec in its theme.</summary>
    public SpecBuilder Builder(Data.DataMembers members) => new(members) { Theme = Colors, Templates = Templates };

    /// <summary>The desktop's brushes, pens and texts for a theme: in its font, or the chart's inherited font (the window's).</summary>
    public ChartPaints Paints(Specs.ChartSpec spec) => Paints(spec.Colors);

    private ChartPaints Paints(ChartTheme theme) =>
        new(new Typeface(theme.FontFamily is { } family ? FontFamily.Parse(family) : TextElement.GetFontFamily(_chart)), TextElement.GetFontSize(_chart)) { Theme = theme, Resolve = Templates.Resolve };

    private void Changed()
    {
        _colors = null;
        Templates.Clear();
        _chart.Retheme();
    }

    private void Adopt(IList? removed, IList? added)
    {
        foreach (var element in removed?.OfType<ILogical>() ?? [])
        {
            _logical.Remove(element);
        }

        foreach (var element in (added?.OfType<ILogical>() ?? []).Where(element => !_logical.Contains(element)))
        {
            _logical.Add(element);
        }
    }
}
