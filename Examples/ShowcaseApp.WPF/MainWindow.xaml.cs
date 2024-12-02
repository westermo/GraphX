using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Controls;
using ShowcaseApp.WPF.Controls;
using ShowcaseApp.WPF.Models;

namespace ShowcaseApp.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Westermo GraphX for WPF showcase application v" +
                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
#if DEBUG
            var lg = new LinkGroup { DisplayName = "Debug" };
            lg.Links.Add(new Link
                { DisplayName = "Debug", Source = new Uri("Pages/Debug/DebugGraph.xaml", UriKind.Relative) });
            MenuLinkGroups.Add(lg);
#endif
            CommandBindings.Add(new CommandBinding(LinkCommands.ShowMiniSpecialDialog, OnShowMiniSpecialDialog,
                OnCanShowMiniSpecialDialog));
        }

        private void OnShowMiniSpecialDialog(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new ModernDialog
            {
                Title = "Help & code window",
                Content = new SpecialWindowControl(e.Parameter),
                ResizeMode = ResizeMode.CanResize,
                MaxWidth = 1920,
                MaxHeight = 1080,
                MinWidth = 700,
                MinHeight = 500,
                Width = 700,
                Height = 500,
                SizeToContent = SizeToContent.Manual,
                OkButton =
                {
                    Content = "OK",
                    VerticalContentAlignment = VerticalAlignment.Center
                }
            };
            //dlg.OkButton.FontWeight = FontWeights.Bold;
            dlg.Buttons = [dlg.OkButton];
            dlg.ShowDialog();
        }

        private void OnCanShowMiniSpecialDialog(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}