using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LivePie : UserControl
{
    public LivePie() => AvaloniaXamlLoader.Load(this);
}
