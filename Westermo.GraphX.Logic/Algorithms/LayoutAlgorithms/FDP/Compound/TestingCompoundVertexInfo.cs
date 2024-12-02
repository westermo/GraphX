using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
    public class TestingCompoundVertexInfo(
        Vector springForce,
        Vector repulsionForce,
        Vector gravityForce,
        Vector applicationForce)
    {
        public Vector SpringForce { get; set; } = springForce;
        public Vector RepulsionForce { get; set; } = repulsionForce;
        public Vector GravityForce { get; set; } = gravityForce;
        public Vector ApplicationForce { get; set; } = applicationForce;
    }
}
