using System.Windows.Media;

namespace ShowcaseApp.WPF.Models;

public class ColorModel(string text, Color color)
{
    public Color Color {get;set;} = color;
    public string Text { get; set; } = text;
}