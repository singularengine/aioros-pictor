using System.Buffers;
using System.Collections.Immutable;
using Aioros.Graphs.Svg;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Content;

/// <summary>Writes custom content as SVG for one item: each node's element, its attributes with their holes filled, and the nodes inside it.</summary>
internal static class ContentMarkup
{
    public static void Write(Svg.MarkupBuffer output, ImmutableArray<ContentNode> nodes, in ContentItem item, string? variant)
    {
        foreach (var node in nodes)
        {
            if (item.Shows(node))
            {
                Node(output, node, item, variant);
            }
        }
    }

    private static void Node(Svg.MarkupBuffer output, ContentNode node, in ContentItem item, string? variant)
    {
        var tag = Tag(node.Element);
        Raw(output, "<"u8);
        Raw(output, tag);
        if (Painting.ClassNames.Tokens(node.Class) is { } classes)
        {
            Attribute(output, " class"u8, classes);
        }

        ContentProp? text = null;
        foreach (var attribute in node.Attributes)
        {
            if (attribute.Name == ContentNames.Content)
            {
                text = attribute;
                continue;
            }

            AttributeOf(output, node.Element, attribute, item, variant);
        }

        if (node.Children.IsDefaultOrEmpty && text is null)
        {
            Raw(output, "/>"u8);
            return;
        }

        Raw(output, ">"u8);
        if (text is not null)
        {
            TextOf(output, text, item);
        }

        Write(output, node.Children, item, variant);
        Raw(output, "</"u8);
        Raw(output, tag);
        Raw(output, ">"u8);
    }

    private static ReadOnlySpan<byte> Tag(ContentElement element) => element switch
    {
        ContentElement.Group => "g"u8,
        ContentElement.Svg => "svg"u8,
        ContentElement.Rect => "rect"u8,
        ContentElement.Circle or ContentElement.Dot => "circle"u8,
        ContentElement.Ellipse => "ellipse"u8,
        ContentElement.Line => "line"u8,
        ContentElement.Polyline => "polyline"u8,
        ContentElement.Polygon => "polygon"u8,
        ContentElement.Text => "text"u8,
        _ => "path"u8,
    };

    private static void AttributeOf(Svg.MarkupBuffer output, ContentElement element, ContentProp attribute, in ContentItem item, string? variant)
    {
        switch (attribute.Name)
        {
            case ContentNames.Path:
                Raw(output, " d=\""u8);
                PathData(output, attribute, item);
                Raw(output, "\""u8);
                return;
            case ContentNames.Shape:
                Raw(output, " d=\""u8);
                ShapeData(output, element, attribute, item);
                Raw(output, "\""u8);
                return;
            case ContentNames.Points:
                Raw(output, " points=\""u8);
                PointsOf(output, attribute, item);
                Raw(output, "\""u8);
                return;
            case ContentNames.Transform:
                Raw(output, " transform=\""u8);
                TransformOf(output, attribute, item);
                Raw(output, "\""u8);
                return;
        }

        if (attribute.Numbers.IsDefaultOrEmpty && Unwritten(attribute, item))
        {
            return;
        }

        Raw(output, " "u8);
        Svg.Utf8Markup.Text(output, attribute.Name);
        Raw(output, "=\""u8);
        if (!attribute.Numbers.IsDefaultOrEmpty)
        {
            for (var i = 0; i < attribute.Numbers.Length; i++)
            {
                Raw(output, i == 0 ? ""u8 : " "u8);
                Number(output, attribute.Name, item.Evaluate(attribute.Numbers[i]));
            }
        }
        else if (attribute.Name is "fill" or "stroke" && Value(attribute, item) is { } paint)
        {
            Svg.Utf8Markup.Text(output, Svg.GradientMarkup.Paint(paint, variant));
        }
        else
        {
            TextOf(output, attribute, item);
        }

        Raw(output, "\""u8);
    }

    /// <summary>Whether a text attribute is left out: its member or its parts give no text, or a paint gives no color.</summary>
    private static bool Unwritten(ContentProp attribute, in ContentItem item) =>
        Value(attribute, item) is not { } value
            ? attribute.Member >= 0 || !attribute.Parts.IsDefaultOrEmpty
            : attribute.Name is "fill" or "stroke" && !Painting.CssValues.IsColor(value);

    /// <summary>An attribute's text: its member's, its parts with their holes filled, or its literal.</summary>
    public static string? Value(ContentProp attribute, in ContentItem item) =>
        attribute.Member >= 0 ? item.Text(attribute.Member) : attribute.Parts.IsDefaultOrEmpty ? attribute.Text : Joined(attribute.Parts, item);

    private static void TextOf(Svg.MarkupBuffer output, ContentProp attribute, in ContentItem item)
    {
        if (attribute.Parts.IsDefaultOrEmpty)
        {
            Svg.Utf8Markup.Text(output, Value(attribute, item));
            return;
        }

        foreach (var part in attribute.Parts)
        {
            switch (part.Kind)
            {
                case ContentPartKind.Text:
                    Svg.Utf8Markup.Text(output, item.Text(part.Member));
                    break;
                case ContentPartKind.Number:
                    NumberText(output, Number(part, item));
                    break;
                case ContentPartKind.Formatted:
                    Svg.Utf8Markup.Text(output, ContentTexts.Format(part.Literal, Number(part, item)));
                    break;
                default:
                    Svg.Utf8Markup.Text(output, part.Literal);
                    break;
            }
        }
    }

    /// <summary>A text's parts joined, for the desktop and for paints.</summary>
    public static string Joined(ImmutableArray<ContentPart> parts, in ContentItem item)
    {
        var text = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            text.Append(part.Kind switch
            {
                ContentPartKind.Text => item.Text(part.Member),
                ContentPartKind.Number => ContentTexts.Number(Number(part, item)),
                ContentPartKind.Formatted => ContentTexts.Format(part.Literal, Number(part, item)),
                _ => part.Literal,
            });
        }

        return text.ToString();
    }

    private static double Number(ContentPart part, in ContentItem item) => part.Value is { } value ? item.Evaluate(value) : double.NaN;

    /// <summary>A number of an attribute: an opacity to the last digit, as Recharts writes the number it is given, a coordinate rounded as Pictor writes every coordinate.</summary>
    private static void Number(Svg.MarkupBuffer output, string name, double value)
    {
        if (name is "opacity" or "fill-opacity" or "stroke-opacity")
        {
            Svg.Utf8Markup.Text(output, ContentTexts.Number(value));
            return;
        }

        Svg.Utf8Markup.Number(output, value);
    }

    private static void NumberText(Svg.MarkupBuffer output, double value) => Svg.Utf8Markup.Text(output, ContentTexts.Number(value));

    private static void PathData(Svg.MarkupBuffer output, ContentProp attribute, in ContentItem item)
    {
        var letters = attribute.Text ?? string.Empty;
        var numbers = attribute.Numbers;
        var n = 0;
        foreach (var letter in letters)
        {
            output.Write([(byte)letter]);
            var count = ContentNames.Arity(letter);
            for (var i = 0; i < count && n < numbers.Length; i++, n++)
            {
                if (i > 0)
                {
                    Raw(output, i % 2 == 0 && letter != 'A' ? " "u8 : ","u8);
                }

                Svg.Utf8Markup.Number(output, item.Evaluate(numbers[n]));
            }
        }
    }

    private static void PointsOf(Svg.MarkupBuffer output, ContentProp attribute, in ContentItem item)
    {
        if (attribute.Member >= 0)
        {
            var points = item.Points;
            for (var i = 0; i + 1 < points.Length; i += 2)
            {
                Raw(output, i == 0 ? ""u8 : " "u8);
                Svg.Utf8Markup.Number(output, points[i]);
                Raw(output, ","u8);
                Svg.Utf8Markup.Number(output, points[i + 1]);
            }

            return;
        }

        for (var i = 0; i + 1 < attribute.Numbers.Length; i += 2)
        {
            Raw(output, i == 0 ? ""u8 : " "u8);
            Svg.Utf8Markup.Number(output, item.Evaluate(attribute.Numbers[i]));
            Raw(output, ","u8);
            Svg.Utf8Markup.Number(output, item.Evaluate(attribute.Numbers[i + 1]));
        }
    }

    private static void TransformOf(Svg.MarkupBuffer output, ContentProp attribute, in ContentItem item)
    {
        var n = 0;
        var first = true;
        foreach (var operation in attribute.Text ?? string.Empty)
        {
            Raw(output, first ? ""u8 : " "u8);
            first = false;
            Raw(output, operation switch
            {
                't' => "translate("u8,
                'r' => "rotate("u8,
                's' => "scale("u8,
                _ => "matrix("u8,
            });
            var count = ContentNames.TransformArity(operation);
            for (var i = 0; i < count && n < attribute.Numbers.Length; i++, n++)
            {
                Raw(output, i == 0 ? ""u8 : ", "u8);
                Svg.Utf8Markup.Number(output, item.Evaluate(attribute.Numbers[n]));
            }

            Raw(output, ")"u8);
        }
    }

    private static void ShapeData(Svg.MarkupBuffer output, ContentElement element, ContentProp attribute, in ContentItem item)
    {
        var sink = new SvgPathWriter(output, 3);
        Span<double> values = stackalloc double[Math.Min(attribute.Numbers.Length, 16)];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = item.Evaluate(attribute.Numbers[i]);
        }

        ContentShapes.Write(ref sink, element, values, attribute.Text, attribute.Member >= 0 ? item.Points : default);
    }
}
