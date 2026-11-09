using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LiveLine : UserControl
{
    public LiveLine() => AvaloniaXamlLoader.Load(this);
}
