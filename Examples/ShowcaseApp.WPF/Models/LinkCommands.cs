using System.Windows.Input;

namespace ShowcaseApp.WPF.Models;

public static class LinkCommands
{
    private static readonly RoutedUICommand ShowMiniSpecialDialogInternal = new("Show help", "ShowMiniSpecialDialog", typeof(LinkCommands));

    /// <summary>
    /// Gets the navigate link routed command.
    /// </summary>
    public static RoutedUICommand ShowMiniSpecialDialog => ShowMiniSpecialDialogInternal;
}