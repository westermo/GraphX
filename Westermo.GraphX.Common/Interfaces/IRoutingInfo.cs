using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Common.Interfaces
{
    public interface IRoutingInfo
    {
        /// <summary>
        /// Routing points collection used to make Path visual object
        /// </summary>
        Point[] RoutingPoints { get; set; }
    }
}
