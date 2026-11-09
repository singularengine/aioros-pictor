using System.Buffers;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Svg;

/// <summary>What the framework writes at a mark of a chart's markup: a field's name, or a command's button.</summary>
internal enum MarkKind : byte
{
    Field,
    Command,
    Option,
}

/// <summary>A place in a chart's markup where the framework writes the attributes of a field or a command, with its key and parameter.</summary>
internal readonly record struct Mark(int Offset, MarkKind Kind, string Key, string? Parameter);

/// <summary>
/// The marks of one render: the SVG canvas notes where a field (a brush's range input) or a command (an item's button)
/// goes, and the markup is copied to the framework's output with the field and command attributes written at each, so
/// the kept markup of a chart replays them on every request and the ids stay the framework's.
/// </summary>
internal sealed class ChartMarks(MarkupBuffer buffer)
{
    private readonly List<Mark> _marks = [];

    public void Field(string key) => _marks.Add(new Mark(buffer.Length, MarkKind.Field, key, null));

    public void Command(string key, string parameter) => _marks.Add(new Mark(buffer.Length, MarkKind.Command, key, parameter));

    /// <summary>An option of a choice field with its value (a nest treemap's node, its zoom).</summary>
    public void Option(string key, string value) => _marks.Add(new Mark(buffer.Length, MarkKind.Option, key, value));

    public Mark[] ToArray() => [.. _marks];

    /// <summary>Copies marked markup to a slot renderer's output, writing each field's and command's attributes at its mark.</summary>
    public static void Write(ref SlotOutput output, ReadOnlySpan<byte> markup, ReadOnlySpan<Mark> marks)
    {
        var start = 0;
        foreach (var mark in marks)
        {
            output.Buffer.Write(markup[start..mark.Offset]);
            switch (mark.Kind)
            {
                case MarkKind.Field:
                    output.WriteField(mark.Key);
                    break;
                case MarkKind.Option:
                    output.WriteFieldOption(mark.Key, mark.Parameter!);
                    break;
                default:
                    output.WriteCommand(mark.Key, mark.Parameter!);
                    break;
            }

            start = mark.Offset;
        }

        output.Buffer.Write(markup[start..]);
    }

    /// <summary>Copies marked markup outside the framework (a test, a benchmark, a static page): each mark as a data attribute naming its key and parameter.</summary>
    public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> markup, ReadOnlySpan<Mark> marks)
    {
        var output = MarkupBuffer.Over(writer);
        var start = 0;
        foreach (var mark in marks)
        {
            output.Write(markup[start..mark.Offset]);
            Utf8Markup.Attribute(output, mark.Kind switch { MarkKind.Field => " data-pictor-field"u8, MarkKind.Option => " data-pictor-option"u8, _ => " data-pictor-command"u8 }, mark.Kind == MarkKind.Field ? mark.Key : mark.Key + "|" + mark.Parameter);
            start = mark.Offset;
        }

        output.Write(markup[start..]);
        output.Flush();
    }
}
