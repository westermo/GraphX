using Avalonia.Media;

namespace ShowcaseApp.Avalonia.Models;

public class ColorModel(string text, Color color)
{
    public Color Color {get;set;} = color;
    public string Text { get; set; } = text;
}