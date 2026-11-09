using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Aioros.Pictor.Tests.Audit;

/// <summary>Templates of the audit's custom content cases.</summary>
public sealed partial class AuditTemplates : UserControl
{
    public AuditTemplates() => AvaloniaXamlLoader.Load(this);

    public IDataTemplate this[string key] => (IDataTemplate)Resources[key]!;
}

/// <summary>Converters of the audit's cases.</summary>
public static class AuditConverters
{
    /// <summary>Shown at depth 1 and below depth 100: two ranges of a whole number.</summary>
    public static IValueConverter OneOrDeep { get; } = new FuncValueConverter<int, bool>(static depth => depth == 1 || depth > 100);
}
