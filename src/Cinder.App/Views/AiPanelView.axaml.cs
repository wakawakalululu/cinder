using Avalonia.Controls;

namespace Cinder.App.Views;

/// <summary>
/// The docked AI assistant panel. Backed by <see cref="Cinder.App.ViewModels.AiPanelViewModel"/>;
/// the shell shows it in the rightmost column, toggled with Ctrl+Shift+A.
/// </summary>
public partial class AiPanelView : UserControl
{
    public AiPanelView()
    {
        InitializeComponent();
    }
}
