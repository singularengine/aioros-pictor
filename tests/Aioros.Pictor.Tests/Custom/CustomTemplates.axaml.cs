using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Aioros.Pictor.Tests.Custom;

/// <summary>The XAML templates of the custom content examples, as their Gallery page declares them.</summary>
public sealed partial class CustomTemplates : UserControl
{
    public CustomTemplates() => AvaloniaXamlLoader.Load(this);

    public IDataTemplate this[string key] => (IDataTemplate)Resources[key]!;
}

/// <summary>Converters with logic, which the template compiler refuses.</summary>
public static class TestConverters
{
    /// <summary>A number's square root.</summary>
    public static IValueConverter Root { get; } = new FuncValueConverter<double, double>(Math.Sqrt);
}
