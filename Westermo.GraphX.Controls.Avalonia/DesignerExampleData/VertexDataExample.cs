using Avalonia.Media;
using Avalonia.Media.Imaging;
using Westermo.GraphX.Common.Models;

namespace Westermo.GraphX.Controls.DesignerExampleData;

internal sealed class VertexDataExample : VertexBase
{
    public VertexDataExample(int id, string name)
    {
        ID = id;
        Name = name;
        DataImage = new Bitmap(@"pack://application:,,,/Westermo.GraphX.Controls;component/Images/help_black.png");
    }

    public string Name { get; set; }
    public IImage DataImage { get; set; }

    public override string ToString()
    {
        return Name;
    }
}