using System.Buffers;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// Recharts' <c>syncId</c> without script: every chart of a sync id carries its class (<c>pictor-y-</c> and the id) and
/// rules scoped to the whole page show, in every chart of the id, the hover state matching the one hovered in any of
/// them: the same index (Recharts' <c>syncMethod="index"</c>), or the same category, through a class per category value
/// on the hit and tip groups (<c>syncMethod="value"</c>). The rules read the same in every chart, as a page's styles
/// are global, so charts written apart still sync; on an Aioros page they are contributed to the page under a key
/// (<see cref="SyncRules"/>), written once in its head however many charts of the id it holds, and a chart written
/// alone carries them inline. Precedent: the legend toggles' and the tooltip's <c>:has()</c> rules; styled-components'
/// style registry.
/// </summary>
internal static class SyncMarkup
{
    /// <summary>
    /// The class of a sync id, one per id and never the same for two ids: its ASCII letters, digits and dashes as they are,
    /// an underscore doubled, and any other character as an underscore, its code in hexadecimal and an underscore
    /// (<c>a.b</c> is <c>pictor-y-a_2e_b</c>, <c>売上</c> is <c>pictor-y-_58f2__4e0a_</c>).
    /// </summary>
    public static string Class(string syncId)
    {
        var name = new System.Text.StringBuilder("pictor-y-", 9 + syncId.Length);
        foreach (var c in syncId)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '-')
            {
                name.Append(c);
            }
            else if (c == '_')
            {
                name.Append("__");
            }
            else
            {
                name.Append('_').Append(((int)c).ToString("x", System.Globalization.CultureInfo.InvariantCulture)).Append('_');
            }
        }

        return name.ToString();
    }

    /// <summary>A category value's class (FNV-1a of its text), the same in every chart.</summary>
    public static string ValueClass(string? value)
    {
        var hash = 2166136261u;
        foreach (var c in value ?? string.Empty)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return "pictor-v" + hash.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The categories a chart syncs over: a categorical chart's categories as shown, none for the others.</summary>
    public static string?[]? Categories(IChartPlot plot) => plot switch
    {
        ChartLayout { ItemTooltip: false, BarTooltip: false, Categories: { } axis } => axis.Categories,
        PolarLayout { Spec.Kind: not Specs.ChartKind.Pie, ItemBars: false } polar => polar.Categories,
        _ => null,
    };

    /// <summary>
    /// The page-wide rules of a chart's sync id, over its hover states or the classes of its categories' values: a state
    /// hovered while no hit group of the page has visible focus (a tap leaves its <c>:hover</c> where the finger was), or a
    /// state with visible focus (the framework's touch follow, the keyboard), shows in every chart of the id.
    /// </summary>
    public static void Rules(MarkupBuffer output, string syncClass, string?[] categories, bool byValue)
    {
        Raw(output, "<style>"u8);
        Body(output, syncClass, Keys(categories, byValue), categories.Length);
        Raw(output, "</style>"u8);
    }

    /// <summary>The classes of the distinct category values a chart syncing by value links its states by, none when it syncs by index.</summary>
    public static string[]? Keys(string?[] categories, bool byValue) =>
        byValue ? categories.Select(ValueClass).Distinct(StringComparer.Ordinal).ToArray() : null;

    /// <summary>The rule set of <see cref="Rules"/> without its element.</summary>
    public static void Body(MarkupBuffer output, string syncClass, string[]? keys, int count)
    {
        count = keys?.Length ?? count;
        Entered(output, syncClass, keys, count, ":root:not(:has(.recharts-wrapper :focus-visible)){"u8, ":hover) ."u8);
        Entered(output, syncClass, keys, count, ":root{"u8, ":focus-visible) ."u8);
    }

    private static void Entered(MarkupBuffer output, string syncClass, string[]? keys, int count, ReadOnlySpan<byte> scope, ReadOnlySpan<byte> entered)
    {
        Raw(output, scope);
        foreach (var tip in (bool[])[true, false])
        {
            for (var i = 0; i < count; i++)
            {
                Selector(output, syncClass, keys, i, entered, tip);
            }

            Raw(output, "{display:inline}"u8);
        }

        Raw(output, "}"u8);
    }

    /// <summary>One state's selector: its hit group entered in any chart of the id, then its tip (or its cursor) in every chart of it.</summary>
    private static void Selector(MarkupBuffer output, string syncClass, string[]? keys, int index, ReadOnlySpan<byte> entered, bool tip)
    {
        Raw(output, index == 0 ? "&:has(."u8 : ",&:has(."u8);
        Utf8Markup.Text(output, syncClass);
        Raw(output, " "u8);
        State(output, keys, index, "pictor-hk"u8);
        Raw(output, entered);
        Utf8Markup.Text(output, syncClass);
        Raw(output, " "u8);
        State(output, keys, index, tip ? "pictor-tk"u8 : "pictor-c"u8);
    }

    /// <summary>A state's group: its index under the layer's prefix (<c>pictor-hk</c> for hits, <c>pictor-tk</c> for tips), or its category value's class in that layer's groups.</summary>
    private static void State(MarkupBuffer output, string[]? keys, int index, ReadOnlySpan<byte> prefix)
    {
        if (keys is not null)
        {
            Raw(output, prefix.SequenceEqual("pictor-c"u8) ? ".pictor-cursors>."u8 : "[class^="u8);
            if (!prefix.SequenceEqual("pictor-c"u8))
            {
                Raw(output, prefix);
                Raw(output, "]."u8);
            }

            Utf8Markup.Text(output, keys[index]);
            return;
        }

        Raw(output, "."u8);
        Raw(output, prefix);
        Number(output, index);
    }
}

/// <summary>
/// A sync id's page-wide rules for one set of hover states, contributed to an Aioros page under a key naming what they
/// cover: the id's class and the number of states (<c>pictor-sync-a.i7</c>), or, by value, a hash of the category
/// values' classes in ordinal order (<c>pictor-sync-b.v</c> and 16 hex digits, the same whatever order the categories
/// come in). Charts of the id over the same states contribute the same key, so the page writes their rules once; charts over other states add their own. Built on first use, at most once
/// per page and data version.
/// </summary>
internal sealed class SyncRules
{
    private readonly string _syncClass;
    private readonly string[]? _keys;
    private readonly int _count;

    public SyncRules(string syncClass, string?[] categories, bool byValue)
    {
        (_syncClass, _keys, _count) = (syncClass, SyncMarkup.Keys(categories, byValue)?.Order(StringComparer.Ordinal).ToArray(), categories.Length);
        var id = syncClass["pictor-y-".Length..];
        Key = _keys is null
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"pictor-sync-{id}.i{_count}")
            : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"pictor-sync-{id}.v{Hash(_keys):x16}");
    }

    /// <summary>The key the rules are contributed under.</summary>
    public string Key { get; }

    /// <summary>The rule set.</summary>
    public string Css => field ??= Build();

    private string Build()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var markup = MarkupBuffer.Over(buffer);
        SyncMarkup.Body(markup, _syncClass, _keys, _count);
        markup.Flush();
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>FNV-1a over the classes, each followed by a separator.</summary>
    private static ulong Hash(string[] keys)
    {
        var hash = 14695981039346656037ul;
        foreach (var key in keys)
        {
            foreach (var c in key)
            {
                hash = (hash ^ c) * 1099511628211ul;
            }

            hash = (hash ^ ' ') * 1099511628211ul;
        }

        return hash;
    }
}
