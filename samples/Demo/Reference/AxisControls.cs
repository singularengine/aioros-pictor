using System.Globalization;
using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor.Demo;

/// <summary>The axis controls of a cartesian spec.</summary>
internal static class AxisControls
{
    /// <summary>An axis control with the spec's props.</summary>
    public static Axis Of(Axis axis, AxisSpec spec)
    {
        PresentationControls.Of(axis, spec.Presentation);
        axis.DataKey = spec.DataKey;
        axis.Type = spec.Type;
        axis.Orientation = spec.Orientation;
        axis.Scale = spec.Scale;
        axis.Hide = spec.Hide;
        axis.Name = spec.Name;
        axis.Unit = spec.Unit;
        axis.TickFormat = spec.TickFormat;
        axis.TickCount = spec.TickCount;
        axis.AllowDecimals = spec.AllowDecimals;
        axis.AllowDataOverflow = spec.AllowDataOverflow;
        axis.Domain = spec.CategoryDomain.IsEmpty ? spec.DomainMin is null && spec.DomainMax is null ? null : $"{spec.DomainMin ?? "0"}, {spec.DomainMax ?? "auto"}" : string.Join(", ", spec.CategoryDomain);
        axis.Padding = spec.Orientation is AxisOrientation.Left or AxisOrientation.Right ? new Thickness(0, spec.PaddingEnd, 0, spec.PaddingStart) : new Thickness(spec.PaddingStart, 0, spec.PaddingEnd, 0);
        axis.PaddingKind = spec.PaddingKind;
        axis.Reversed = spec.Reversed;
        axis.Mirror = spec.Mirror;
        axis.Interval = spec.Interval switch
        {
            TickInterval.Fixed => spec.IntervalCount.ToString(CultureInfo.InvariantCulture),
            var interval => char.ToLowerInvariant(interval.ToString()[0]) + interval.ToString()[1..],
        };
        axis.MinTickGap = spec.MinTickGap;
        axis.TickSize = spec.TickSize;
        axis.TickMargin = spec.TickMargin;
        axis.TickLine = spec.TickLine;
        axis.AxisLine = spec.AxisLine;
        axis.Tick = spec.Tick;
        axis.Stroke = spec.Stroke;
        axis.Ticks = spec.Ticks.IsEmpty ? null : string.Join(", ", spec.Ticks);
        axis.AllowDuplicatedCategory = spec.AllowDuplicatedCategory;
        axis.IncludeHidden = spec.IncludeHidden;
        axis.Angle = spec.Angle;
        axis.TextAnchor = spec.TextAnchor;
        axis.TickFill = spec.TickFill;
        axis.TickFontSize = spec.TickFontSize;
        axis.AxisLineStroke = spec.AxisLineStyle?.Stroke;
        axis.AxisLineStrokeWidth = spec.AxisLineStyle?.StrokeWidth;
        axis.AxisLineStrokeDashArray = spec.AxisLineStyle?.StrokeDashArray;
        axis.TickLineStroke = spec.TickLineStyle?.Stroke;
        axis.TickLineStrokeWidth = spec.TickLineStyle?.StrokeWidth;
        axis.TickLineStrokeDashArray = spec.TickLineStyle?.StrokeDashArray;
        CartesianControls.Labelled(axis, spec.Label);
        SpecControls.AddLabels(axis.Labels, spec.Labels);
        axis.Label = spec.Label?.Value;
        axis.LabelPosition = spec.Label?.Position;
        if (axis is XAxis x && spec.Size > 0)
        {
            x.Height = spec.Size;
        }
        else if (axis is YAxis y && spec.Size > 0)
        {
            y.Width = spec.Size;
        }

        return axis;
    }
}
