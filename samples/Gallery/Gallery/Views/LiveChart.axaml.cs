using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views;

public sealed partial class LiveChart : UserControl
{
    public LiveChart() => AvaloniaXamlLoader.Load(this);
}
