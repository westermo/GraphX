namespace ShowcaseApp.WPF.Content;

/// <summary>
/// Interaction logic for SettingsAppearance.xaml
/// </summary>
public partial class SettingsAppearance
{
    public SettingsAppearance()
    {
        InitializeComponent();

        // a simple view model for appearance configuration
        DataContext = new SettingsAppearanceViewModel();
    }
}