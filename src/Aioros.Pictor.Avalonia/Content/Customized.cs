using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' <c>Customized</c> as a chart's child: custom content drawn with the chart's layout, in its place among the
/// chart's children. Its template's data context is the chart's <see cref="LayoutContext"/> (its size and its plot's box).
/// </summary>
public sealed class Customized : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="Template"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> TemplateProperty = AvaloniaProperty.Register<Customized, IDataTemplate?>(nameof(Template));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The content (Recharts' <c>component</c>): a template whose data context is the chart's <see cref="LayoutContext"/>.</summary>
    [Content]
    public IDataTemplate? Template { get => GetValue(TemplateProperty); set => SetValue(TemplateProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order)
    {
        if (builder.Content(Template, Content.ContentSlots.Layout, this, nameof(Template)) is { } content)
        {
            builder.Customized.Add(new CustomizedSpec { Content = content, Order = order });
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}
