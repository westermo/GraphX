using Westermo.GraphX.Common.Models;

namespace Westermo.GraphX.Controls.Avalonia.DesignerExampleData
{
    internal sealed class EdgeDataExample<TVertex> : EdgeBase<TVertex>
    {
        public EdgeDataExample(TVertex source, TVertex target)
            : base(source, target)
        {
            
        }
        public EdgeDataExample(TVertex source, TVertex target, double weight)
            : base(source, target, weight)
        {
            
        }

        public string Text { get; set; } = "";
    }
}
