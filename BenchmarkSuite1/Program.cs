using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

namespace GraphXBenchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddExporter(JsonExporter.Full)
                .AddExporter(JsonExporter.FullCompressed);
            
            // Run all benchmark suites
            // BenchmarkRunner.Run<GraphRenderingBenchmarks>(config);
            // BenchmarkRunner.Run<OptimizationBenchmarks>(config);
            BenchmarkRunner.Run<LayoutAlgorithmBenchmarks>(config);
            // BenchmarkRunner.Run<EdgeRoutingBenchmarks>(config);
            // BenchmarkRunner.Run<OverlapRemovalBenchmarks>(config);
        }
    }
}
