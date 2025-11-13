using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.EdgeRouting;

public class MatrixItem(Point pt, bool inter, int placeX, int placeY)
{
    public Point Point = pt;
    public bool IsIntersected = inter;

    public int PlaceX = placeX;
    public int PlaceY = placeY;

    public int Weight => IsIntersected ? 0 : 1;
}