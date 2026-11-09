using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aioros.Pictor.Tests.Desktop;

public sealed partial class BoundChartView : UserControl
{
    public BoundChartView() => AvaloniaXamlLoader.Load(this);

    internal LineChart LineChart => this.FindControl<LineChart>("Chart")!;
}

public sealed record BoundItem(string Name, double Value);

public sealed class BoundChartModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<BoundItem> Items { get; } = [new("A", 1), new("B", 3), new("C", 2)];

    public string Color
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        }
    } = "#8884d8";
}
