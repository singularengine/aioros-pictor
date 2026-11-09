using System.Globalization;
using Aioros.Pictor.Content;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Aioros.Pictor;

/// <summary>
/// The values one probe gives a template's context: every number member (the slot's, then the data members the
/// template reads), and every text member, marked so that the compiler recognizes them in what Avalonia lays out.
/// </summary>
internal sealed class ProbePoint
{
    private static int _next;

    private ProbePoint(List<double> numbers, string?[] texts, int id)
    {
        Numbers = numbers;
        Texts = texts;
        Id = id;
    }

    public List<double> Numbers { get; }

    public string?[] Texts { get; }

    public int Id { get; }

    /// <summary>The base point: distinct numbers, and texts, colors and anchors marked with the probe's id.</summary>
    public static ProbePoint Base(ContentSlot slot)
    {
        var numbers = new List<double>(slot.Numbers.Length);
        for (var i = 0; i < slot.Numbers.Length; i++)
        {
            numbers.Add(BaseOf(slot.Numbers[i].Kind, i));
        }

        var id = Interlocked.Increment(ref _next);
        return new ProbePoint(numbers, Marks(slot, id), id);
    }

    /// <summary>The base value of a number member: distinct whole numbers, so that a text showing one names it.</summary>
    public static double BaseOf(MemberKind kind, int index) => kind switch
    {
        MemberKind.Whole => 1001 + (2 * index),
        MemberKind.Flag => 1,
        _ => 300 + (37 * index),
    };

    /// <summary>The base value of a data member the template reads, by its index among the number members.</summary>
    public static double PayloadBase(int member) => 7000 + (53 * member);

    /// <summary>A copy with fresh marks on its texts, a number member changed.</summary>
    public ProbePoint With(ContentSlot slot, int member, double value)
    {
        var copy = Copy(slot);
        copy.Set(member, value);
        return copy;
    }

    /// <summary>A copy with fresh marks on its texts.</summary>
    public ProbePoint Copy(ContentSlot slot)
    {
        var id = Interlocked.Increment(ref _next);
        return new ProbePoint([.. Numbers], Marks(slot, id), id);
    }

    public void Set(int member, double value)
    {
        while (Numbers.Count <= member)
        {
            Numbers.Add(PayloadBase(Numbers.Count));
        }

        Numbers[member] = value;
    }

    /// <summary>A text member's mark: a unique token for texts, a unique color for colors, an anchor for anchors.</summary>
    private static string?[] Marks(ContentSlot slot, int id)
    {
        var texts = new string?[slot.Texts.Length];
        for (var j = 0; j < texts.Length; j++)
        {
            texts[j] = slot.Texts[j].Kind switch
            {
                MemberKind.Color => string.Create(CultureInfo.InvariantCulture, $"#{(17 + (j * 29)) % 256:x2}{id % 256:x2}{(97 + (id / 256)) % 256:x2}"),
                MemberKind.Anchor => ((string[])["start", "end", "middle"])[(id + j) % 3],
                _ => string.Create(CultureInfo.InvariantCulture, $"¤{id}·{j}¤"),
            };
        }

        return texts;
    }
}

/// <summary>
/// Lays a detached template out again from scratch: every element's measure and arrange invalidated, so a shape stretched
/// to a new size computes its transform again (a detached tree has no layout manager to follow the invalidations), and
/// without layout rounding, as SVG places its elements at fractions of a pixel.
/// </summary>
internal static class ContentLayout
{
    public static void Run(Canvas host)
    {
        Invalidate(host);
        host.Measure(new Size(100000, 100000));
        host.Arrange(new Rect(0, 0, 100000, 100000));
    }

    private static void Invalidate(Avalonia.Visual visual)
    {
        if (visual is Avalonia.Layout.Layoutable layoutable)
        {
            layoutable.UseLayoutRounding = false;
            layoutable.InvalidateMeasure();
            layoutable.InvalidateArrange();
        }

        foreach (var child in Avalonia.VisualTree.VisualExtensions.GetVisualChildren(visual))
        {
            Invalidate(child);
        }
    }
}

/// <summary>
/// Lays a template out for probes: one instance of it in a canvas whose logical and inheritance parent is the chart's
/// element (so its resources, styles and inherited font reach it), its context replaced by each probe's values; the data
/// members it reads are noted as it reads them.
/// </summary>
internal sealed class ContentProbes : IDisposable
{
    private readonly ContentSlot _slot;
    private readonly Canvas _host = new();
    private readonly Control _instance;
    private ProbePoint? _current;

    public ContentProbes(IDataTemplate template, ContentSlot slot, StyledElement? anchor)
    {
        _slot = slot;
        if (anchor is not null)
        {
            ((ISetLogicalParent)_host).SetParent(anchor);
            ((ISetInheritanceParent)_host).SetParent(anchor);
        }

        var first = ProbePoint.Base(slot);
        _instance = template.Build(Context(first)) ?? throw new ContentRefusedException("the template built nothing");
        _host.Children.Add(_instance);
    }

    /// <summary>The data members the template read, in the order it first read them.</summary>
    public List<string> Keys { get; } = [];

    /// <summary>How many number members the probes vary: the slot's and the data members read so far.</summary>
    public int Count => _slot.Numbers.Length + Keys.Count;

    public ContentSlot Slot => _slot;

    /// <summary>The template laid out with a probe's values, read as SVG.</summary>
    public SceneNode Run(ProbePoint point)
    {
        var context = Context(point);
        _instance.DataContext = context;
        ContentLayout.Run(_host);
        return _slot.Html ? HtmlScene.Read(_instance) : ContentScene.Read(_instance);
    }

    public void Dispose()
    {
        _host.Children.Clear();
        ((ISetLogicalParent)_host).SetParent(null);
        ((ISetInheritanceParent)_host).SetParent(null);
    }

    private IContentContext Context(ProbePoint point)
    {
        _current = point;
        var numbers = new double[_slot.Numbers.Length];
        for (var i = 0; i < numbers.Length; i++)
        {
            numbers[i] = point.Numbers[i];
        }

        return ContentContexts.Create(new ContentState(_slot, numbers, (string?[])point.Texts.Clone(), new ContentPayload(key => Payload(key), null))
        {
            Points = _slot.Points ? [point.Numbers[0], point.Numbers[1], point.Numbers[0] + 40, point.Numbers[1] + 10, point.Numbers[0] + 20, point.Numbers[1] + 60] : [],
        });
    }

    /// <summary>A data member's marked value: a whole number per member, noted the first time it is read.</summary>
    private double Payload(string key)
    {
        var index = Keys.IndexOf(key);
        if (index < 0)
        {
            Keys.Add(key);
            index = Keys.Count - 1;
        }

        var member = _slot.Numbers.Length + index;
        var point = _current!;
        return member < point.Numbers.Count ? point.Numbers[member] : ProbePoint.PayloadBase(member);
    }
}
