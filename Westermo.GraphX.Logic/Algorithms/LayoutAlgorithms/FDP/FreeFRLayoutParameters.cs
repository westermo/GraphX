using System;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class FreeFRLayoutParameters : FRLayoutParametersBase
{
    private double _idealEdgeLength = 10;

    public override double K => _idealEdgeLength;

    public override double InitialTemperature => Math.Sqrt(Math.Pow(_idealEdgeLength, 2) * VertexCount);

    /// <summary>
    /// Constant. Represents the ideal length of the edges.
    /// </summary>
    public double IdealEdgeLength
    {
        get => _idealEdgeLength;
        set
        {
            _idealEdgeLength = value;
            UpdateParameters();
        }
    }
}