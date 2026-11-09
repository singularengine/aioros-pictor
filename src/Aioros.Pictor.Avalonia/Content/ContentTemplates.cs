using System.Collections.Immutable;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Aioros.Pictor;

/// <summary>
/// A chart's templates of custom content, each compiled once (per theme, as a template may read the theme's resources)
/// and kept until the chart's theme or resources change; a refused template is drawn on the desktop from an instance
/// laid out by Avalonia with each item's context.
/// </summary>
internal sealed class ContentTemplates
{
    private readonly Dictionary<(IDataTemplate Template, string Slot), ContentSpec> _compiled = [];
    private readonly Dictionary<ContentSpec, Refused> _refused = new(ReferenceEqualityComparer.Instance);

    private sealed record Refused(IDataTemplate Template, ContentSlot Slot, StyledElement Owner)
    {
        public ContentInstance? Instance { get; set; }
    }

    /// <summary>The compiled template of a slot, or null when the slot has none.</summary>
    public ContentSpec? Compile(IDataTemplate? template, ContentSlot slot, StyledElement owner, string property)
    {
        if (template is null)
        {
            return null;
        }

        if (!_compiled.TryGetValue((template, slot.Name), out var content))
        {
            content = ContentCompiler.Compile(template, slot, owner, property);
            _compiled[(template, slot.Name)] = content;
            if (content.Refused is not null)
            {
                _refused[content] = new Refused(template, slot, owner);
            }
        }

        return content;
    }

    /// <summary>Forgets every compiled template (the theme or a resource changed).</summary>
    public void Clear()
    {
        foreach (var refused in _refused.Values)
        {
            refused.Instance?.Dispose();
        }

        _compiled.Clear();
        _refused.Clear();
    }

    /// <summary>What a refused template draws for an item: its instance laid out with the item's context, read as constant nodes.</summary>
    public ImmutableArray<ContentNode> Resolve(ContentSpec content, in ContentItem item)
    {
        if (!_refused.TryGetValue(content, out var refused))
        {
            return [];
        }

        try
        {
            refused.Instance ??= new ContentInstance(refused.Template, refused.Slot, refused.Owner);
            return refused.Instance.Nodes(item);
        }
        catch (ContentRefusedException)
        {
            return [];
        }
    }
}

/// <summary>One instance of a refused template, laid out by Avalonia with each item's context and read as constant nodes.</summary>
internal sealed class ContentInstance : IDisposable
{
    private readonly ContentSlot _slot;
    private readonly Canvas _host = new();
    private readonly Control _instance;

    public ContentInstance(IDataTemplate template, ContentSlot slot, StyledElement owner)
    {
        _slot = slot;
        ((ISetLogicalParent)_host).SetParent(owner);
        ((ISetInheritanceParent)_host).SetParent(owner);
        var context = Context(default);
        _instance = template.Build(context) ?? throw new ContentRefusedException("the template built nothing");
        _host.Children.Add(_instance);
    }

    public ImmutableArray<ContentNode> Nodes(in ContentItem item)
    {
        _instance.DataContext = Context(item);
        ContentLayout.Run(_host);
        return ContentConstants.Nodes(_slot.Html ? HtmlScene.Read(_instance) : ContentScene.Read(_instance)) is { } node ? [node] : [];
    }

    public void Dispose()
    {
        _host.Children.Clear();
        ((ISetLogicalParent)_host).SetParent(null);
        ((ISetInheritanceParent)_host).SetParent(null);
    }

    private IContentContext Context(in ContentItem item)
    {
        var numbers = new double[_slot.Numbers.Length];
        var texts = new string?[_slot.Texts.Length];
        item.Numbers[..Math.Min(numbers.Length, item.Numbers.Length)].CopyTo(numbers);
        item.Texts[..Math.Min(texts.Length, item.Texts.Length)].CopyTo(texts);
        return ContentContexts.Create(new ContentState(_slot, numbers, texts, ContentPayload.Of(item.Datum, Chart.Members)) { Points = item.Points.ToArray() });
    }
}

/// <summary>A laid-out template read as content whose every value is a constant (what Avalonia showed for one item).</summary>
internal static class ContentConstants
{
    public static ContentNode? Nodes(SceneNode scene)
    {
        if (!scene.Visible)
        {
            return null;
        }

        var children = ImmutableArray.CreateBuilder<ContentNode>();
        foreach (var child in scene.Children)
        {
            if (Nodes(child) is { } node)
            {
                children.Add(node);
            }
        }

        return new ContentNode
        {
            Element = scene.Element,
            Class = scene.Class,
            Attributes = Defaults.Leave(scene.Element, [.. scene.Props.Select(Prop)]),
            Children = children.ToImmutable(),
        };
    }

    private static ContentProp Prop(SceneProp prop) => prop.Name == ContentNames.Content && prop.Numbers.Length == 1
        ? new ContentProp { Name = prop.Name, Text = ContentTexts.Format(prop.Format, prop.Numbers[0]) }
        : new ContentProp { Name = prop.Name, Text = prop.Text, Numbers = [.. prop.Numbers.Select(number => new ContentNumber { Constant = number })] };
}
