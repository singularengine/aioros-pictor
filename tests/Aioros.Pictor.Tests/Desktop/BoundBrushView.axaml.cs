using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aioros.Pictor.Tests.Desktop;

public sealed partial class BoundBrushView : UserControl
{
    public BoundBrushView() => AvaloniaXamlLoader.Load(this);

    internal BarChart BarChart => this.FindControl<BarChart>("Chart")!;
}

public sealed class BoundBrushModel
{
    public IReadOnlyList<BoundItem> Items { get; } = [new("A", 1), new("B", 3), new("C", 2), new("D", 4)];

    public int? Start { get; set; } = 1;

    public int? End { get; set; } = 2;
}
