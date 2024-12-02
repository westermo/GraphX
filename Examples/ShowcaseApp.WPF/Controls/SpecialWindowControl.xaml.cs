using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Windows;
using ShowcaseApp.WPF.Models;

namespace ShowcaseApp.WPF.Controls
{
    /// <summary>
    /// Interaction logic for SpecialWindowControl.xaml
    /// </summary>
    public partial class SpecialWindowControl
    {
        public SpecialWindowControl(object type)
        {
            InitializeComponent();
            tabControl.ContentLoader = new SpecialContentLoader((MiniSpecialType?)type ?? MiniSpecialType.None);
        }
    }

    internal class SpecialContentLoader(MiniSpecialType type) : IContentLoader
    {
        public MiniSpecialType OpType { get; private set; } = type;

        public Task<object> LoadContentAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                throw new InvalidOperationException(Resources.UIThreadRequired);

            // scheduler ensures LoadContent is executed on the current UI thread
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            return Task.Factory.StartNew(() => LoadContent(uri), cancellationToken, TaskCreationOptions.None,
                scheduler);
        }

        protected virtual object LoadContent(Uri uri)
        {
            // don't do anything in design mode
            if (ModernUIHelper.IsInDesignMode) return null;

            var result = Application.LoadComponent(uri);
            if (result is ISpecialWindowContentIntro spContent)
                spContent.IntroText = Properties.Resources.ResourceManager.GetString(OpType + "Text");
            if (result is ISpecialWindowContentXaml spContent2)
                spContent2.XamlText = Properties.Resources.ResourceManager.GetString(OpType.ToString());
            if (result is ISpecialWindowContentXamlTemplate spContent3)
            {
                var xamlTemplate = Properties.Resources.ResourceManager.GetString(OpType + "Template");
                if (string.IsNullOrEmpty(xamlTemplate))
                    xamlTemplate = Properties.Resources.ResourceManager.GetString("CommonMiniTemplate");
                spContent3.XamlText = xamlTemplate;
            }

            return result;
        }
    }

    internal interface ISpecialWindowContentIntro
    {
        string IntroText { get; set; }
    }

    internal interface ISpecialWindowContentXaml
    {
        string XamlText { get; set; }
    }

    internal interface ISpecialWindowContentXamlTemplate
    {
        string XamlText { get; set; }
    }
}