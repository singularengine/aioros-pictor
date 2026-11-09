using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls.Documents;

namespace Aioros.Pictor;

/// <summary>The spec a chart's own props and its children build, which the web writer receives and the desktop lays out.</summary>
internal static class ChartSpecs
{
    /// <summary>The children's props, in the order Recharts draws them.</summary>
    public static SpecBuilder Build(SpecBuilder builder, IEnumerable<IChartElement> elements)
    {
        var order = 0;
        foreach (var element in elements)
        {
            if (element is ISpecSource source)
            {
                source.Apply(builder, order);
            }

            order++;
        }

        return builder;
    }

    /// <summary>The chart's spec at a size from its own props and its children's, before its family adds its own.</summary>
    public static ChartSpec Of(Chart chart, ChartKind kind, double width, double height, SpecBuilder builder)
    {
        var padding = chart.Padding;
        width = width > 0 ? width : 500;
        height = chart.Aspect is var aspect and > 0 && double.IsNaN(chart.Height) ? chart.AspectHeight(width, aspect) : height;
        return new ChartSpec
        {
            Kind = kind,
            Width = width,
            Height = height > 0 ? height : 300,
            Responsive = double.IsNaN(chart.Width) || double.IsNaN(chart.Height),
            Aspect = chart.Aspect > 0 ? chart.Aspect : HeightOnly(chart) && string.IsNullOrEmpty(chart.ResponsiveWidths) ? width / height : null,
            FixedHeight = HeightOnly(chart) && !string.IsNullOrEmpty(chart.ResponsiveWidths),
            MinWidth = chart.MinWidth > 0 ? chart.MinWidth : null,
            MinHeight = chart.MinHeight > 0 ? chart.MinHeight : null,
            MaxHeight = double.IsFinite(chart.MaxHeight) ? chart.MaxHeight : null,
            Margin = new Edges(padding.Top, padding.Right, padding.Bottom, padding.Left),
            FontSize = TextElement.GetFontSize(chart),
            Grid = builder.Grid,
            XAxes = [.. builder.XAxes],
            YAxes = [.. builder.YAxes],
            Series = [.. builder.Series],
            ReferenceLines = [.. builder.References],
            Legend = builder.Legend,
            Tooltip = builder.Tooltip,
            Title = chart.Title,
            Desc = chart.Desc,
            ContainerId = chart.ContainerId,
            ContainerClass = chart.ContainerClass,
            ContainerStyle = chart.ContainerStyle,
            AccessibilityLayer = chart.AccessibilityLayer,
            ClassName = ChangeRelay.ClassName(chart),
            WrapperStyle = chart.WrapperStyle,
            Role = chart.Role,
            TabIndex = chart.IsSet(Avalonia.Input.InputElement.TabIndexProperty) ? chart.TabIndex : null,
            ResponsiveWidths = SpecText.Numbers(chart.ResponsiveWidths),
            Concealed = chart.Pointer.Concealed,
            Theme = builder.SpecTheme,
            Customized = [.. builder.Customized],
        };
    }

    /// <summary>The height that holds a chart keeping an aspect ratio with its legend and axes, when that is more than it has; 0 otherwise.</summary>
    public static double FittedHeight(Chart chart, double width, double height) =>
        chart.Elements.OfType<Legend>().Any() && Layout.ChartFit.Needed(Painting.Plots.Compute(chart.Spec(width, height), Data.ChartData.From(chart.Data, Chart.Members))) is { } needed ? needed : 0;

    private static bool HeightOnly(Chart chart) => double.IsNaN(chart.Width) && !double.IsNaN(chart.Height) && !(chart.Aspect > 0);

    /// <summary>The chart's commands and sync id added to its spec.</summary>
    public static ChartSpec Interactive(Chart chart, ChartSpec spec) => spec with
    {
        ClickCommand = chart.IsSet(Chart.ClickCommandProperty) ? ChartSlot.Click : null,
        ItemClickCommand = chart.IsSet(Chart.ItemClickCommandProperty) ? ChartSlot.ItemClick : null,
        SyncId = chart.SyncId,
        SyncMethod = chart.SyncId is null || chart.SyncMethod == "index" ? null : chart.SyncMethod,
    };

    /// <summary>A chart with the accessibility layer takes focus.</summary>
    public static void Focus(StyledProperty<bool> layer) =>
        layer.Changed.AddClassHandler<Chart>(static (chart, change) => chart.Focusable = change.GetNewValue<bool>());
}
