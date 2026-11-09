using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views.Live;

public sealed partial class LiveBar : UserControl
{
    public LiveBar() => AvaloniaXamlLoader.Load(this);
}
