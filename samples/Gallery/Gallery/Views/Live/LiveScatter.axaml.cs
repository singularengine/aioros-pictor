using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LiveScatter : UserControl
{
    public LiveScatter() => AvaloniaXamlLoader.Load(this);
}
