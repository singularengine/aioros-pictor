using System.Globalization;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Aioros.Pictor;

/// <summary>The HTML side of <see cref="ContentScene"/>: a tooltip's or a legend's laid-out template read as <c>div</c>s and <c>p</c>s.</summary>
internal static class HtmlScene
{
    /// <summary>
    /// Reads a laid-out template of a tooltip's or a legend's content as HTML: panels and borders as <c>div</c>s (a
    /// stack panel's orientation and spacing as a flex direction and gap, a border's padding, border, background and
    /// shadow, a fixed or minimum size), text blocks as <c>p</c>s with their line height and font family, each with its
    /// margin and its alignment; the browser lays them out, so no position is read.
    /// </summary>
    public static SceneNode Read(Control root) => Html(root);

    private static SceneNode Html(Control control) => Checked(control) switch
    {
        LegendItems items => Items(items),
        TooltipItems lines => Lines(lines),
        Border border => HtmlBorder(border),
        StackPanel stack => HtmlPanel(stack, stack.Children, stack.Orientation == Orientation.Horizontal ? "row" : "column", stack.Spacing),
        Panel panel => HtmlPanel(panel, panel.Children, "column", 0),
        TextBlock text => HtmlText(text),
        _ => throw new ContentRefusedException($"{control.GetType().Name} is not an element Pictor writes as a tooltip's or a legend's HTML (panels, Border and TextBlock are)"),
    };

    private static SceneNode Items(LegendItems items)
    {
        var node = new SceneNode(ContentElement.Items, nameof(LegendItems), items.IsVisible);
        if (items.Orientation == Orientation.Horizontal)
        {
            node.Props.Add(new SceneProp("direction", [], "row"));
            node.Props.Add(new SceneProp("gap", [items.Spacing], null));
            node.Props.Add(new SceneProp("basis", [double.IsNaN(items.ItemBasis) ? 0 : items.ItemBasis], null));
        }

        return node;
    }

    private static SceneNode Lines(TooltipItems lines)
    {
        var node = new SceneNode(ContentElement.Items, nameof(TooltipItems), lines.IsVisible);
        node.Props.Add(new SceneProp("gap", [lines.Spacing], null));
        return node;
    }

    private static Control Checked(Control control)
    {
        ContentScene.BoundBrushes(control);
        return control;
    }

    private static SceneNode HtmlPanel(Control panel, Controls children, string direction, double gap)
    {
        var node = new SceneNode(ContentElement.Division, panel.GetType().Name, panel.IsVisible) { Class = SvgElement.GetClass(panel) };
        node.Props.Add(new SceneProp("direction", [], direction));
        node.Props.Add(new SceneProp("gap", [gap], null));
        Box(node, panel.Margin, default, null, null, 0, 0);
        Sized(node, panel);
        ContentScene.Opacity(node, panel);
        foreach (var child in children)
        {
            node.Children.Add(Html(child));
        }

        return node;
    }

    private static SceneNode HtmlBorder(Border border)
    {
        var node = new SceneNode(ContentElement.Division, nameof(Avalonia.Controls.Border), border.IsVisible) { Class = SvgElement.GetClass(border) };
        node.Props.Add(new SceneProp("direction", [], "column"));
        node.Props.Add(new SceneProp("gap", [0], null));
        Box(node, border.Margin, border.Padding, border.Background, border.BorderBrush, border.BorderBrush is null ? 0 : border.BorderThickness.Left, border.CornerRadius.TopLeft);
        Sized(node, border);
        if (SvgElement.GetBorderStyle(border) is { } style && style is "dashed" or "dotted")
        {
            node.Props.Add(new SceneProp("border-style", [], style));
        }

        if (border.BoxShadow.Count > 0)
        {
            node.Props.Add(new SceneProp("box-shadow", [], Shadows(border.BoxShadow)));
        }

        ContentScene.Opacity(node, border);
        if (border.Child is { } child)
        {
            node.Children.Add(Html(child));
        }

        return node;
    }

    private static void Box(SceneNode node, Thickness margin, Thickness padding, IBrush? background, IBrush? border, double width, double radius)
    {
        node.Props.Add(new SceneProp("margin", [margin.Top, margin.Right, margin.Bottom, margin.Left], null));
        node.Props.Add(new SceneProp("padding", [padding.Top, padding.Right, padding.Bottom, padding.Left], null));
        node.Props.Add(new SceneProp("background", [], ContentScene.Translucent(background)));
        node.Props.Add(new SceneProp("border-color", [], ContentScene.Translucent(border)));
        node.Props.Add(new SceneProp("border-width", [width], null));
        node.Props.Add(new SceneProp("border-radius", [radius], null));
    }

    private static void Sized(SceneNode node, Control control)
    {
        if (!double.IsNaN(control.Width))
        {
            node.Props.Add(new SceneProp("width", [control.Width], null));
        }

        if (!double.IsNaN(control.Height))
        {
            node.Props.Add(new SceneProp("height", [control.Height], null));
        }

        if (control.MinWidth > 0)
        {
            node.Props.Add(new SceneProp("min-width", [control.MinWidth], null));
        }

        Aligned(node, control);
    }

    private static void Aligned(SceneNode node, Control control)
    {
        if (control.HorizontalAlignment != HorizontalAlignment.Stretch)
        {
            node.Props.Add(new SceneProp("h-align", [], control.HorizontalAlignment.ToString().ToLowerInvariant()));
        }

        if (control.VerticalAlignment != VerticalAlignment.Stretch)
        {
            node.Props.Add(new SceneProp("v-align", [], control.VerticalAlignment.ToString().ToLowerInvariant()));
        }
    }

    /// <summary>A border's shadows as CSS's <c>box-shadow</c>: each one's offsets, blur and spread in pixels and its color with its translucency.</summary>
    private static string Shadows(BoxShadows shadows)
    {
        var parts = new List<string>(shadows.Count);
        foreach (var shadow in shadows)
        {
            parts.Add(Shadow(shadow));
        }

        return string.Join(", ", parts);
    }

    private static string Shadow(BoxShadow shadow) =>
        $"{(shadow.IsInset ? "inset " : string.Empty)}{Length(shadow.OffsetX)} {Length(shadow.OffsetY)} {Length(shadow.Blur)} {Length(shadow.Spread)} {ContentScene.Color(shadow.Color, Math.Round(shadow.Color.A / 255.0, 4))}";

    private static string Length(double value) => value == 0 ? "0" : string.Create(CultureInfo.InvariantCulture, $"{value}px");

    /// <summary>A font family as Avalonia resolves it (an embedded font's source and name, <c>avares://App/Assets/Fonts#Geist Mono</c>), which the web writes by its names.</summary>
    private static string Family(FontFamily family) => family.Key is { Source: { } source } ? $"{source.OriginalString}#{family.Name}" : family.Name;

    private static SceneNode HtmlText(TextBlock text)
    {
        var node = new SceneNode(ContentElement.Paragraph, nameof(Avalonia.Controls.TextBlock), text.IsVisible) { Class = SvgElement.GetClass(text) };
        var margin = text.Margin;
        node.Props.Add(new SceneProp("margin", [margin.Top, margin.Right, margin.Bottom, margin.Left], null));
        if (text.IsSet(Avalonia.Controls.TextBlock.ForegroundProperty))
        {
            node.Props.Add(new SceneProp("color", [], ContentScene.Translucent(text.Foreground)));
        }

        if (text.IsSet(Avalonia.Controls.TextBlock.FontSizeProperty))
        {
            node.Props.Add(new SceneProp("font-size", [text.FontSize], null));
        }

        if (text.FontWeight != Avalonia.Media.FontWeight.Normal)
        {
            node.Props.Add(new SceneProp("font-weight", [], ((int)text.FontWeight).ToString(CultureInfo.InvariantCulture)));
        }

        if (!double.IsNaN(text.LineHeight))
        {
            node.Props.Add(new SceneProp("line-height", [text.LineHeight], null));
        }

        if (text.IsSet(Avalonia.Controls.TextBlock.FontFamilyProperty))
        {
            node.Props.Add(new SceneProp("font-family", [], Family(text.FontFamily)));
        }

        Aligned(node, text);
        ContentScene.Opacity(node, text);
        node.Props.Add(new SceneProp(ContentNames.Content, [], text.Text ?? string.Empty));
        return node;
    }
}
