using System.Buffers;
using System.Globalization;
using System.Net;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

[assembly: SlotWriter<Gallery.Compare.PictorReferenceWriter>]
[assembly: SlotWriter<Gallery.Compare.RechartsReferenceWriter>]

namespace Gallery.Compare;

/// <summary>
/// A Recharts reference example drawn by Pictor, by name: on the web Pictor's writer writes the chart from the
/// example's spec and data (the markup the tests compare with Recharts'), on the desktop the chart control built from
/// the same spec draws it.
/// </summary>
public sealed class PictorReference : ContentControl, IWrittenBySlot
{
    public static readonly StyledProperty<string?> ExampleProperty = AvaloniaProperty.Register<PictorReference, string?>(nameof(Example));

    public string? Example
    {
        get => GetValue(ExampleProperty);
        set => SetValue(ExampleProperty, value);
    }

    public WrittenSlot? WrittenSlot => WrittenSlot.Of<PictorReferenceWriter>(string.Empty, nameof(Example));

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExampleProperty)
        {
            Content = ReferenceExamples.Find(Example) is { } example ? SpecControls.Create(example) : null;
        }
    }
}

/// <summary>Writes a reference example's chart with Pictor's writer.</summary>
public sealed class PictorReferenceWriter : ISlotWriter
{
    public static SlotWrite Create(string argument, MemberLookup members) => static (value, output) =>
    {
        if (ReferenceExamples.Find(value as string) is { } example)
        {
            ChartSvg.Write(output, example.Spec, example.Data, static (_, _) => null);
        }
    };
}

/// <summary>Recharts' own render of a reference example, by name: its screenshot in Chromium, an image served by the app on the web and read from the file on the desktop.</summary>
public sealed class RechartsReference : ContentControl, IWrittenBySlot
{
    public static readonly StyledProperty<string?> ExampleProperty = AvaloniaProperty.Register<RechartsReference, string?>(nameof(Example));

    public string? Example
    {
        get => GetValue(ExampleProperty);
        set => SetValue(ExampleProperty, value);
    }

    public WrittenSlot? WrittenSlot => WrittenSlot.Of<RechartsReferenceWriter>(string.Empty, nameof(Example));

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExampleProperty)
        {
            Content = Example is { } name && File.Exists(ReferenceExamples.Png(name)) ? new Image { Source = new Bitmap(ReferenceExamples.Png(name)), Stretch = Avalonia.Media.Stretch.None } : null;
        }
    }
}

/// <summary>Writes Recharts' screenshot of a reference example as a lazy image at its size.</summary>
public sealed class RechartsReferenceWriter : ISlotWriter
{
    public static SlotWrite Create(string argument, MemberLookup members) => static (value, output) =>
    {
        if (ReferenceExamples.Find(value as string) is not { } example)
        {
            return;
        }

        var name = WebUtility.HtmlEncode(example.Name);
        var html = string.Create(CultureInfo.InvariantCulture, $"<img src=\"/reference/{name}.png\" width=\"{example.Spec.Width}\" height=\"{example.Spec.Height}\" alt=\"Recharts' render of {name}\" loading=\"lazy\" style=\"display:block;max-width:none\">");
        output.Write(Encoding.UTF8.GetBytes(html));
    };
}
