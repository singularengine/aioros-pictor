using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gallery.Models;

/// <summary>A reading whose value changes in place, notifying the change as an Avalonia view model does.</summary>
public sealed class Reading(string name, double value) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; } = name;

    public double Value
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    } = value;
}

/// <summary>Readings changed in place by a command: the same items, new values, and the chart's bound paint, corners, padding and dashes.</summary>
public sealed class BindingData : INotifyPropertyChanged
{
    private static readonly double[] Steps = [4000, 3000, 2000, 2780, 1890, 2390, 3490];

    private static readonly IBrush[] Paints =
    [
        new ImmutableLinearGradientBrush([new ImmutableGradientStop(0, Color.Parse("#8884d8")), new ImmutableGradientStop(1, Color.Parse("#8884d833"))], startPoint: new RelativePoint(0, 0, RelativeUnit.Relative), endPoint: new RelativePoint(0, 1, RelativeUnit.Relative)),
        new ImmutableSolidColorBrush(Color.Parse("#82ca9d")),
        new ImmutableSolidColorBrush(Color.Parse("#ff7300"), 0.75),
    ];

    private static readonly CornerRadius[] Corners = [new(10, 10, 0, 0), new(0), new(6)];

    private static readonly Thickness[] Paddings = [new(20, 5, 30, 5), new(40, 15, 30, 5), new(20, 5, 30, 5)];

    private static readonly double[][] Dashes = [[3, 3], [8, 4], [1, 4]];

    public BindingData() => Step = new StepCommand(this);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The bars' paint: a gradient, then solid colors (an Avalonia brush the web plan follows).</summary>
    public IBrush Paint => Paints[Index % Paints.Length];

    /// <summary>The bars' corners, as Avalonia's corner radius.</summary>
    public CornerRadius Corner => Corners[Index % Corners.Length];

    /// <summary>The chart's padding, as Avalonia's thickness.</summary>
    public Thickness Inner => Paddings[Index % Paddings.Length];

    /// <summary>The grid's dash pattern, as a list of numbers.</summary>
    public double[] Dash => Dashes[Index % Dashes.Length];

    public ObservableCollection<Reading> Readings { get; } = [.. Steps.Select((value, i) => new Reading("Page " + (char)('A' + i), value))];

    public ICommand Step { get; }

    /// <summary>How many steps were taken: a field of the page, so on the web each post carries it and the server renders the next, as the native app's model keeps it.</summary>
    public decimal Count
    {
        get;
        set
        {
            field = value;
            for (var i = 0; i < Readings.Count; i++)
            {
                Readings[i].Value = Steps[(i + Index) % Steps.Length];
            }

            foreach (var name in (string[])[nameof(Count), nameof(Paint), nameof(Corner), nameof(Inner), nameof(Dash)])
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    private int Index => (int)(decimal.Truncate(Math.Abs(Count)) % 21);

    private void Advance() => Count++;

    private sealed class StepCommand(BindingData owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => owner.Advance();
    }
}
