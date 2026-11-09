using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LiveArea : UserControl
{
    public LiveArea() => AvaloniaXamlLoader.Load(this);
}
