window.BENCHMARK_DATA = {
  "lastUpdate": 1769777474541,
  "repoUrl": "https://github.com/westermo/GraphX",
  "entries": {
    "Benchmark.Net Benchmark": [
      {
        "commit": {
          "author": {
            "email": "carl.andersson@westermo.com",
            "name": "caran",
            "username": "carl-andersson-at-westermo"
          },
          "committer": {
            "email": "carl.andersson@westermo.com",
            "name": "caran",
            "username": "carl-andersson-at-westermo"
          },
          "distinct": true,
          "id": "986342e1567964708786c3311229e189bd391ba1",
          "message": "Removed incompatible benchmark attribute.",
          "timestamp": "2026-01-27T10:15:57+01:00",
          "tree_id": "886e6fb6a9edf94be23ede9ca28fdb37be196b2e",
          "url": "https://github.com/westermo/GraphX/commit/986342e1567964708786c3311229e189bd391ba1"
        },
        "date": 1769506470183,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 326450.2048779297,
            "unit": "ns",
            "range": "± 22745.711010402127"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 3233902.925702336,
            "unit": "ns",
            "range": "± 200996.15967277173"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 20628160.738079898,
            "unit": "ns",
            "range": "± 1191123.0232284283"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 1041593.4478125,
            "unit": "ns",
            "range": "± 68267.75001155144"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 16482863.318576388,
            "unit": "ns",
            "range": "± 540437.7454875091"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 108792008.55084746,
            "unit": "ns",
            "range": "± 4816895.157962608"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 1130199.8113644621,
            "unit": "ns",
            "range": "± 41406.31071419016"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 16201082.974358974,
            "unit": "ns",
            "range": "± 834416.2655465547"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 119224894.06854838,
            "unit": "ns",
            "range": "± 5371596.478147575"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 20726361.426666666,
            "unit": "ns",
            "range": "± 1046862.8563039432"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 107827472.37692308,
            "unit": "ns",
            "range": "± 5027089.781698255"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 92745113.48837209,
            "unit": "ns",
            "range": "± 5036512.344094742"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 98206651.55696203,
            "unit": "ns",
            "range": "± 5068813.067438953"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 109726328.921875,
            "unit": "ns",
            "range": "± 3090909.9991231337"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 23050111.54563492,
            "unit": "ns",
            "range": "± 1052423.249115525"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 14283224.04715909,
            "unit": "ns",
            "range": "± 603633.3547477374"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "142813963+carl-andersson-at-westermo@users.noreply.github.com",
            "name": "Caran",
            "username": "carl-andersson-at-westermo"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1fc4bf45368075e24e0e0da7cd3c4ac4ae13f0a1",
          "message": "Performance Optimizations (#13)\n\n* copilot instr.\n\n* global.json\n\n* Add unit tests and implement features for geometry caching, level of detail, object pooling, and viewport culling\n\n- Implemented GeometryCachingTests to validate edge geometry caching functionality.\n- Added LevelOfDetailTests to ensure reasonable defaults and behavior for LOD settings.\n- Created ObjectPoolTests to verify the functionality of object pooling for lists and dictionaries.\n- Developed ViewportCullingTests to test viewport-based visibility culling for graph elements.\n- Introduced BatchUpdateScope and DeferredPositionUpdateScope for efficient edge and vertex updates.\n- Added LevelOfDetailSettings to manage LOD rendering settings for optimizing graph display.\n- Implemented SimplePool for generic object pooling to reduce allocations.\n- Created ViewportCulling class to manage visibility of graph elements based on viewport.\n\n* Established benchmarks for Layout algorithms\n\n* Optimized some layout algss\n\n* Refactor edge routing algorithms for performance improvements and memory efficiency\n\n* Update .github/copilot-instructions.md\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/ViewportCulling.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Logic/Algorithms/LayoutAlgorithms/FDP/KKLayoutAlgorithm.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Logic/Algorithms/EdgeRouting/PathFinderER/PathFinder.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Refactor edge update throttling mechanism for improved performance and responsiveness\n\n* Enhance edge pointer handling and visibility logic for improved rendering and positioning\n\n* Fix for edge pointers\n\n* Add Avalonia test job to NuGet workflow for improved CI process\n\n* Fix command syntax for running Avalonia tests in CI workflow\n\n* Implement multiple selection mode for graph vertices and update selection handling logic\n\n* Enhance documentation for graph controls and view model, adding detailed summaries and remarks for better clarity and maintainability\n\n---------\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>",
          "timestamp": "2026-01-30T13:45:02+01:00",
          "tree_id": "ff0fde8e258b04bfd0890e07c35e81c6178e226f",
          "url": "https://github.com/westermo/GraphX/commit/1fc4bf45368075e24e0e0da7cd3c4ac4ae13f0a1"
        },
        "date": 1769777469410,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 314652.5071466619,
            "unit": "ns",
            "range": "± 9859.268026299726"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 3211857.7864583335,
            "unit": "ns",
            "range": "± 58819.46441479788"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 20885976.41964286,
            "unit": "ns",
            "range": "± 269864.7856463961"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 1086013.3493381077,
            "unit": "ns",
            "range": "± 36142.93018310405"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 15419534.615234375,
            "unit": "ns",
            "range": "± 279390.8824463941"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 104644718.23913044,
            "unit": "ns",
            "range": "± 3969605.626737794"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 1033294.4990425858,
            "unit": "ns",
            "range": "± 41700.48739111799"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 15407030.754375,
            "unit": "ns",
            "range": "± 394929.94046522793"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 88227427.49180327,
            "unit": "ns",
            "range": "± 3571294.775216302"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 19315006.47421875,
            "unit": "ns",
            "range": "± 679386.4035630865"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 101275987.09189188,
            "unit": "ns",
            "range": "± 3376392.558271177"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 102649650.37931034,
            "unit": "ns",
            "range": "± 2981218.297313825"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 104028665.21875,
            "unit": "ns",
            "range": "± 1997016.2833654773"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 105363945.01724137,
            "unit": "ns",
            "range": "± 3043798.13739217"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 20129069.995535713,
            "unit": "ns",
            "range": "± 271351.3163467085"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 13450680.27845982,
            "unit": "ns",
            "range": "± 380638.05552342837"
          }
        ]
      }
    ]
  }
}