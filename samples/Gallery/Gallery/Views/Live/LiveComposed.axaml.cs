using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LiveComposed : UserControl
{
    public LiveComposed() => AvaloniaXamlLoader.Load(this);
}
