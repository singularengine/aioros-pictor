using System.Collections.Immutable;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' <c>Label</c> as a child element of an axis, a reference or a polar radius axis: drawn after the element's
/// own <c>Label</c> text, several per element, its text given as <see cref="Value"/> or as the element's content.
/// </summary>
public sealed class Label : StyledElement, ISeriesPart
{
    /// <summary>Defines <see cref="Value"/>.</summary>
    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<Label, string?>(nameof(Value));

    /// <summary>Defines <see cref="Position"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> PositionProperty = AvaloniaProperty.Register<Label, LabelPosition?>(nameof(Position));

    /// <summary>Defines <see cref="PositionX"/>.</summary>
    public static readonly StyledProperty<string?> PositionXProperty = AvaloniaProperty.Register<Label, string?>(nameof(PositionX));

    /// <summary>Defines <see cref="PositionY"/>.</summary>
    public static readonly StyledProperty<string?> PositionYProperty = AvaloniaProperty.Register<Label, string?>(nameof(PositionY));

    /// <summary>Defines <see cref="Offset"/>.</summary>
    public static readonly StyledProperty<double> OffsetProperty = AvaloniaProperty.Register<Label, double>(nameof(Offset), 5);

    /// <summary>Defines <see cref="Angle"/>.</summary>
    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<Label, double>(nameof(Angle));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Label, string?>(nameof(Fill));

    /// <summary>Defines <see cref="FontSize"/>.</summary>
    public static readonly StyledProperty<double?> FontSizeProperty = AvaloniaProperty.Register<Label, double?>(nameof(FontSize));

    /// <summary>Defines <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<Label, string?>(nameof(Format));

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<Label, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The text (Recharts' <c>value</c>, or the label's children).</summary>
    [Content]
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Where the label sits around its element's box; the center by default.</summary>
    public LabelPosition? Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>With <see cref="PositionY"/>, the label at this x from the box's left (pixels or a percentage of its width), Recharts' <c>position={{x, y}}</c>.</summary>
    public string? PositionX { get => GetValue(PositionXProperty); set => SetValue(PositionXProperty, value); }

    /// <summary>With <see cref="PositionX"/>, the label at this y from the box's top (pixels or a percentage of its height).</summary>
    public string? PositionY { get => GetValue(PositionYProperty); set => SetValue(PositionYProperty, value); }

    /// <summary>The distance from the box, 5 by default.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>The rotation, in degrees.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

    /// <summary>The text's fill; gray (<c>#808080</c>) by default.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The text's own font size.</summary>
    public double? FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>A .NET composite format for the value (Recharts' <c>formatter</c>, by decision): <c>{0} kg</c>.</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>
    /// The custom content drawn in place of the label (Recharts' <c>content</c> element): a template whose data context is
    /// the label's <see cref="LabelContext"/> (its element's box, Recharts' <c>viewBox</c>, and its text). Drawn for the
    /// labels of cartesian axes, references and series; a polar radius axis draws its default label.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>The labels of an element's children, in order.</summary>
    internal static ImmutableArray<LabelSpec> Specs(AvaloniaList<Label> labels, SpecBuilder builder) => [.. labels.Select(label => label.Spec(builder))];

    internal LabelSpec Spec(SpecBuilder builder) => new()
    {
        Content = builder.Content(ContentTemplate, Content.ContentSlots.Label, this, nameof(ContentTemplate)),
        Value = Value,
        Position = Position,
        PositionX = PositionX,
        PositionY = PositionY,
        Offset = Offset,
        Angle = Angle,
        Fill = Fill,
        FontSize = FontSize,
        Format = Format,
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}
