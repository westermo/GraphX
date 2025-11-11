using System.ComponentModel;
using ICSharpCode.AvalonEdit.Folding;

namespace ShowcaseApp.WPF.Controls;

/// <summary>
/// Interaction logic for MiniContentPage.xaml
/// </summary>
public partial class MiniXamlTemplatePage : ISpecialWindowContentXamlTemplate, INotifyPropertyChanged
{
    public MiniXamlTemplatePage()
    {
        InitializeComponent();
        DataContext = this;
        var foldingManager = FoldingManager.Install(textEditor.TextArea);
        var foldingStrategy = new XmlFoldingStrategy();
        foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
        textEditor.Options.HighlightCurrentLine = true;
        textEditor.ShowLineNumbers = true;
    }

    private string _text;

    public string XamlText
    {
        get => _text;
        set
        {
            _text = value;
            textEditor.Text = _text;
            OnPropertyChanged("XamlText");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}