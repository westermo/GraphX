using System;
using System.Collections.Generic;
using System.Threading;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Measure;

namespace ShowcaseApp.Avalonia.ExampleModels
{
    public class ExampleExternalOverlapRemovalAlgorithm: IExternalOverlapRemoval<DataVertex>
    {
        public IDictionary<DataVertex, Rect> Rectangles { get; set; } = new Dictionary<DataVertex, Rect>();

        public void Compute(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
