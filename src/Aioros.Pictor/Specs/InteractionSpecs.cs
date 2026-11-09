namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>Brush</c>: a strip under the x-axis that selects the window of items the chart shows.</summary>
public sealed record BrushSpec
{
    /// <summary>The custom content drawn for each traveller (Recharts' <c>traveller</c> element), compiled from its XAML template.</summary>
    public ContentSpec? TravellerContent { get; init; }

    /// <summary>The data key of the texts at the window's ends.</summary>
    public string? DataKey { get; init; }

    /// <summary>The strip's height.</summary>
    public double Height { get; init; } = 40;

    /// <summary>The color of the frame, the slide and the travellers.</summary>
    public string? Stroke { get; init; }

    /// <summary>The strip's background.</summary>
    public string? Fill { get; init; }

    /// <summary>The width of a traveller (a handle at one end of the window).</summary>
    public double TravellerWidth { get; init; } = 5;

    /// <summary>How many items a drag moves an index by at least.</summary>
    public int Gap { get; init; } = 1;

    /// <summary>Whether the web writes the travellers as two range inputs posted as the framework's fields (Recharts' brush moving in the browser).</summary>
    public bool? Fields { get; init; }

    /// <summary>The first item of the window; the first item when null.</summary>
    public int? StartIndex { get; init; }

    /// <summary>The last item of the window; the last item when null.</summary>
    public int? EndIndex { get; init; }

    /// <summary>The order of the brush among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>A .NET composite format for the texts at the window's ends (Recharts' <c>tickFormatter</c>), given the item's text.</summary>
    public string? TickFormat { get; init; }

    /// <summary>Whether the texts at the window's ends are shown at rest too (Recharts' <c>alwaysShowText</c>).</summary>
    public bool AlwaysShowText { get; init; }

    /// <summary>The travellers' accessible name (Recharts' <c>ariaLabel</c>); Recharts' <c>Min value: …, Max value: …</c> when null.</summary>
    public string? AriaLabel { get; init; }

    /// <summary>How long the end texts stay after the pointer leaves the brush on the desktop, in milliseconds (Recharts' <c>leaveTimeOut</c>).</summary>
    public double LeaveTimeOut { get; init; } = 1000;

    /// <summary>The strip's left edge; the plot's when null.</summary>
    public double? X { get; init; }

    /// <summary>The strip's top edge; under the x-axis when null.</summary>
    public double? Y { get; init; }

    /// <summary>The strip's width; the plot's when null.</summary>
    public double? Width { get; init; }

    /// <summary>The margin of the panorama inside the strip (Recharts' <c>padding</c>), 1 on every side by default.</summary>
    public Edges Padding { get; init; } = new(1, 1, 1, 1);

    /// <summary>A small chart of every item drawn inside the strip (Recharts' panorama, the brush's chart child), if any.</summary>
    public ChartSpec? Panorama { get; init; }
}
