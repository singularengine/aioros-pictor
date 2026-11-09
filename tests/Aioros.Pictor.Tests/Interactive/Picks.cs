using System.Windows.Input;
using Aioros.Pictor.Demo;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Tests.Interactive;

/// <summary>A command that keeps the parameters it ran with.</summary>
internal sealed class Picks : ICommand
{
    public List<string?> Ran { get; } = [];

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => Ran.Add(parameter as string);
}

/// <summary>Writes a chart control as Aioros's plan does, through its slot and a slot output.</summary>
internal static class WebChart
{
    public static readonly MemberLookup Members = static (owner, member) => item => owner.GetProperty(member)?.GetValue(item)
        ?? (item is IReadOnlyDictionary<string, object?> row && row.TryGetValue(member, out var value) ? value : null);

    public static Slots.Rendered Write(Chart chart, double width = 500, double height = 300)
    {
        chart.Measure(new Avalonia.Size(width, height));
        chart.Arrange(new Avalonia.Rect(0, 0, width, height));
        var slot = chart.WrittenSlot!;
        var render = slot.Create(slot.Argument, static (owner, member) => item => item is IReadOnlyDictionary<string, object?> row ? row.GetValueOrDefault(member) : owner.GetProperty(member)?.GetValue(item));
        return Slots.Render(render, chart.Data, new Dictionary<string, object?>());
    }

    public static int Count(string html, string text) => (html.Length - html.Replace(text, string.Empty, StringComparison.Ordinal).Length) / text.Length;

    public static Chart Create(RechartsExample example) => SpecControls.Create(example);
}
