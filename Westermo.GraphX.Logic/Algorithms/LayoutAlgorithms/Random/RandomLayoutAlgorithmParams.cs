using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class RandomLayoutAlgorithmParams: LayoutParametersBase
{
    /// <summary>
    /// Gets or sets layout bounds 
    /// </summary>
    public Rect Bounds { get; set; } = new(0, 0, 2000, 2000);
}