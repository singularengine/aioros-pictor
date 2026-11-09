using System.Buffers;
using System.Text;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Tests;

/// <summary>Runs Pictor's chart renderer as Aioros's plan does, through a slot output over a buffer.</summary>
internal static class Slots
{
    /// <summary>The renderer of an argument as a writer of a bound value alone (no bound parts, no fields or commands).</summary>
    public static SlotWrite Writer(string argument, MemberLookup members)
    {
        var render = ChartRenderer.Create(argument, members);
        return (value, buffer) =>
        {
            var output = new SlotOutput(new HtmlOutput(buffer), value);
            render(ref output);
        };
    }

    /// <summary>One render of a renderer with the values the plan reads for its bound parts, as the markup and the fields and commands it reserved, its page's rule sets collected in <paramref name="styles"/>.</summary>
    public static Rendered Render(SlotRender render, object? value, IReadOnlyDictionary<string, object?> bound, PageStyles? styles = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var parts = new PlanParts(bound);
        var output = new SlotOutput(new HtmlOutput(buffer) { Styles = styles ?? new PageStyles() }, value, parts);
        render(ref output);
        return new Rendered(Encoding.UTF8.GetString(buffer.WrittenSpan), parts.Reserved);
    }

    /// <summary>A render's markup and its reservations: a field's key, or a command's key and parameter, by id.</summary>
    internal sealed record Rendered(string Html, List<(int Id, string Key, string? Parameter)> Reserved);

    /// <summary>The parts a plan reads: the values of the bound ones, and the ids of the fields and of the commands' buttons, each numbered apart in render order.</summary>
    private sealed class PlanParts(IReadOnlyDictionary<string, object?> bound) : ISlotParts
    {
        public List<(int Id, string Key, string? Parameter)> Reserved { get; } = [];

        public bool TryRead(string key, object? scope, out object? value) => bound.TryGetValue(key, out value);

        public void WriteFieldId(ref HtmlOutput output, string key, object? scope)
        {
            var id = output.ReserveField(null, scope);
            Reserved.Add((id, key, null));
            output.WriteFieldId(id);
        }

        public void WriteCommandId(ref HtmlOutput output, string key, string parameter, object? scope)
        {
            var id = output.ReserveButton(null, scope);
            Reserved.Add((id, key, parameter));
            output.WriteFieldId(id);
        }

        public int WriteOptionId(ref HtmlOutput output, string key, string value, int reserved, object? scope)
        {
            var id = reserved < 0 ? output.ReserveField(null, scope) : reserved;
            Reserved.Add((id, key, value));
            output.WriteFieldId(id);
            return id;
        }
    }
}
