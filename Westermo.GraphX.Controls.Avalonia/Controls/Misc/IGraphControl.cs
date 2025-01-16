using System.Windows;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Controls.Avalonia
{
    public interface IGraphControl : IPositionChangeNotify
    {
        GraphAreaBase? RootArea { get; }
        Point GetPosition(bool final = false, bool round = false);
        void SetPosition(Point pt, bool alsoFinal = true);
        void SetPosition(double x, double y, bool alsoFinal = true);
        bool IsVisible { get; set; }
        void Clean();
    }
}