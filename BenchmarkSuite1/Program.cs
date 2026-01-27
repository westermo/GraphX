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
            var _ = BenchmarkRunner.Run<GraphRenderingBenchmarks>(config);
        }
    }
}
