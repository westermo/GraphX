using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Westermo.GraphX.Logic.Algorithms.OverlapRemoval;
using Westermo.GraphX.Measure;

namespace GraphXBenchmarks;

/// <summary>
/// Benchmarks for overlap removal algorithms which are used to separate overlapping vertices
/// after layout computation.
/// </summary>
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class OverlapRemovalBenchmarks
{
    public sealed class BenchObject
    {
        public int Id { get; init; }
        public override string ToString() => $"Obj{Id}";
    }

    // Rectangle sets with varying degrees of overlap
    private Dictionary<BenchObject, Rect> _minimalOverlapRects = null!;    // Few overlaps
    private Dictionary<BenchObject, Rect> _moderateOverlapRects = null!;   // Moderate overlaps
    private Dictionary<BenchObject, Rect> _heavyOverlapRects = null!;      // Many overlaps
    private Dictionary<BenchObject, Rect> _largeSetRects = null!;          // Large number of rectangles

    [GlobalSetup]
    public void Setup()
    {
        // Create rectangle sets with different overlap characteristics
        _minimalOverlapRects = CreateRectangles(50, 0.1);   // 10% overlap probability
        _moderateOverlapRects = CreateRectangles(50, 0.4);  // 40% overlap probability
        _heavyOverlapRects = CreateRectangles(50, 0.8);     // 80% overlap probability
        _largeSetRects = CreateRectangles(200, 0.5);        // 200 rectangles, 50% overlap
    }

    private static Dictionary<BenchObject, Rect> CreateRectangles(int count, double overlapProbability)
    {
        var random = new Random(42);
        var rects = new Dictionary<BenchObject, Rect>();
        
        const double rectWidth = 60;
        const double rectHeight = 40;
        
        // Calculate spacing based on overlap probability
        // Higher probability = tighter packing
        var spacing = rectWidth * (1 - overlapProbability * 0.8);
        var gridSize = (int)Math.Ceiling(Math.Sqrt(count));

        for (var i = 0; i < count; i++)
        {
            var obj = new BenchObject { Id = i };
            
            var gridX = i % gridSize;
            var gridY = i / gridSize;
            
            // Add some random jitter
            var jitterX = (random.NextDouble() - 0.5) * rectWidth * overlapProbability;
            var jitterY = (random.NextDouble() - 0.5) * rectHeight * overlapProbability;
            
            var x = gridX * spacing + jitterX;
            var y = gridY * spacing + jitterY;
            
            rects[obj] = new Rect(x, y, rectWidth, rectHeight);
        }

        return rects;
    }

    private static Dictionary<BenchObject, Rect> CloneRectangles(Dictionary<BenchObject, Rect> source)
    {
        // Clone to avoid modifying original during benchmark
        return new Dictionary<BenchObject, Rect>(source);
    }

    #region FSA Algorithm Benchmarks

    [Benchmark(Description = "FSA - 50 rects, minimal overlap")]
    public IDictionary<BenchObject, Rect> FSA_MinimalOverlap()
    {
        var rects = CloneRectangles(_minimalOverlapRects);
        var algorithm = new FSAAlgorithm<BenchObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "FSA - 50 rects, moderate overlap")]
    public IDictionary<BenchObject, Rect> FSA_ModerateOverlap()
    {
        var rects = CloneRectangles(_moderateOverlapRects);
        var algorithm = new FSAAlgorithm<BenchObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "FSA - 50 rects, heavy overlap")]
    public IDictionary<BenchObject, Rect> FSA_HeavyOverlap()
    {
        var rects = CloneRectangles(_heavyOverlapRects);
        var algorithm = new FSAAlgorithm<BenchObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "FSA - 200 rects")]
    public IDictionary<BenchObject, Rect> FSA_LargeSet()
    {
        var rects = CloneRectangles(_largeSetRects);
        var algorithm = new FSAAlgorithm<BenchObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    #endregion

    #region OneWayFSA Algorithm Benchmarks

    [Benchmark(Description = "OneWayFSA Horizontal - 50 rects, minimal")]
    public IDictionary<BenchObject, Rect> OneWayFSA_H_Minimal()
    {
        var rects = CloneRectangles(_minimalOverlapRects);
        var algorithm = new OneWayFSAAlgorithm<BenchObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "OneWayFSA Horizontal - 50 rects, heavy")]
    public IDictionary<BenchObject, Rect> OneWayFSA_H_Heavy()
    {
        var rects = CloneRectangles(_heavyOverlapRects);
        var algorithm = new OneWayFSAAlgorithm<BenchObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "OneWayFSA Vertical - 50 rects, heavy")]
    public IDictionary<BenchObject, Rect> OneWayFSA_V_Heavy()
    {
        var rects = CloneRectangles(_heavyOverlapRects);
        var algorithm = new OneWayFSAAlgorithm<BenchObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Vertical });
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "OneWayFSA - 200 rects")]
    public IDictionary<BenchObject, Rect> OneWayFSA_LargeSet()
    {
        var rects = CloneRectangles(_largeSetRects);
        var algorithm = new OneWayFSAAlgorithm<BenchObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    #endregion

    #region Comparative Benchmarks

    [Benchmark(Baseline = true, Description = "FSA (Baseline) - 50 rects")]
    public IDictionary<BenchObject, Rect> Baseline_FSA()
    {
        var rects = CloneRectangles(_moderateOverlapRects);
        var algorithm = new FSAAlgorithm<BenchObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    [Benchmark(Description = "OneWayFSA vs FSA - 50 rects")]
    public IDictionary<BenchObject, Rect> Compare_OneWayFSA()
    {
        var rects = CloneRectangles(_moderateOverlapRects);
        var algorithm = new OneWayFSAAlgorithm<BenchObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);
        return algorithm.Rectangles;
    }

    #endregion
}
