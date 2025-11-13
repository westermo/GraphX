using System.ComponentModel;

namespace ShowcaseApp.WPF.Controls;

/// <summary>
/// Interaction logic for MiniContentPage.xaml
/// </summary>
public partial class MiniContentPage : ISpecialWindowContentIntro, INotifyPropertyChanged
{
    public MiniContentPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private string _text;

    public string IntroText
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged(nameof(IntroText));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}