window.BENCHMARK_DATA = {
  "lastUpdate": 1771937736313,
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
          "id": "3e93d90a2e7e3e73521a6ba1af63b3627e25d0d9",
          "message": "Refactor edge rendering methods and improve layout invalidation (#14)\n\n* Refactor edge rendering methods and improve layout invalidation\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/GraphArea.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Examples/ShowcaseApp.Avalonia/Pages/PerformanceGraph.axaml.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Fix endpoint override handling in edge dragging logic\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControl.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Fix edge rendering and label positioning in GraphArea and EdgeControlBase\n\n* Refactor edge pointer visibility handling and improve drag logic in EdgeControl\n\n---------\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>",
          "timestamp": "2026-02-23T10:51:12+01:00",
          "tree_id": "a75c0bf0d4fc2d14d38f8ed55cca1d64c2104862",
          "url": "https://github.com/westermo/GraphX/commit/3e93d90a2e7e3e73521a6ba1af63b3627e25d0d9"
        },
        "date": 1771840545453,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 269084.912109375,
            "unit": "ns",
            "range": "± 3778.7753909454395"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2901253.734375,
            "unit": "ns",
            "range": "± 39807.14890055101"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 18797676.137019232,
            "unit": "ns",
            "range": "± 236152.19788640097"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 867289.2738882211,
            "unit": "ns",
            "range": "± 7507.874108596983"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 12754415.253125,
            "unit": "ns",
            "range": "± 203200.69638279185"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 78928525.8125,
            "unit": "ns",
            "range": "± 2863468.2468835167"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 874786.154296875,
            "unit": "ns",
            "range": "± 4178.9024955940795"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 13131252.563541668,
            "unit": "ns",
            "range": "± 186381.23072661503"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 75927452.07692307,
            "unit": "ns",
            "range": "± 2307818.9312675656"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 18868982.401442308,
            "unit": "ns",
            "range": "± 207381.99987074998"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 77274946.41025642,
            "unit": "ns",
            "range": "± 2694062.4727202477"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 78542048.22619048,
            "unit": "ns",
            "range": "± 2743711.6944320286"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 97478606.92999999,
            "unit": "ns",
            "range": "± 1564246.7559781165"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 77013883.1491228,
            "unit": "ns",
            "range": "± 3302314.927811256"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 19855778.81919643,
            "unit": "ns",
            "range": "± 168326.7912511973"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 12348176.62139423,
            "unit": "ns",
            "range": "± 70986.11396055222"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "carl.andersson@westermo.com",
            "name": "Carl Andersson",
            "username": "carl-andersson-at-westermo"
          },
          "committer": {
            "email": "carl.andersson@westermo.com",
            "name": "Carl Andersson",
            "username": "carl-andersson-at-westermo"
          },
          "distinct": true,
          "id": "74b7bf22087b0a62fa6679d5a857df25832db913",
          "message": "Fix relayout to actually position things.",
          "timestamp": "2026-02-23T17:22:35+01:00",
          "tree_id": "16840a9d896fa7243f2901a6300b8d15fd02bd58",
          "url": "https://github.com/westermo/GraphX/commit/74b7bf22087b0a62fa6679d5a857df25832db913"
        },
        "date": 1771864702083,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 336209.3731445313,
            "unit": "ns",
            "range": "± 4700.529398447757"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 3330597.8359375,
            "unit": "ns",
            "range": "± 30116.680458158546"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 20314664.89955357,
            "unit": "ns",
            "range": "± 153345.08794202277"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 682507.6104910715,
            "unit": "ns",
            "range": "± 8342.876328969205"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 8109640.165625,
            "unit": "ns",
            "range": "± 117384.54284021848"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 61951312.23611111,
            "unit": "ns",
            "range": "± 1268175.1990030638"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 689679.4736979167,
            "unit": "ns",
            "range": "± 11363.239994164283"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 8117641.146205357,
            "unit": "ns",
            "range": "± 92304.15841764092"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 63155554.777173914,
            "unit": "ns",
            "range": "± 1533419.7288407586"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 20003834.379166666,
            "unit": "ns",
            "range": "± 295617.1563524864"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 65595900.663461536,
            "unit": "ns",
            "range": "± 613925.3086942025"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 60688267.6,
            "unit": "ns",
            "range": "± 1034359.0099458448"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 64814610.044117644,
            "unit": "ns",
            "range": "± 1278623.7232388936"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 61991036.6875,
            "unit": "ns",
            "range": "± 1173180.2774267225"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 21349863.90848214,
            "unit": "ns",
            "range": "± 203911.31491538082"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 8364414.868566177,
            "unit": "ns",
            "range": "± 171251.22163823945"
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
          "id": "1750afe8f95c869935a45447f903a4e2bd9be543",
          "message": "Adjusted EdgeLabelControls to actually move and render along the edge properly, and improved performance (#15)\n\n* Adjusted EdgeLabelControls to actually move and render along the edge properly, and improved performance\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControlBase.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControlBase.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControlBase.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeLabels/EdgeLabelControl.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControlBase.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Small fixes\n\n* Removed bad UpdateLayout logic from EdgePointers\n\n* Cleanup\n\n* Order change to prevent glitching\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgePointers/DefaultEdgePointer.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Update Westermo.GraphX.Controls.Avalonia/Controls/EdgeControlBase.cs\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>\n\n* Cleanup\n\n---------\n\nCo-authored-by: Copilot <175728472+Copilot@users.noreply.github.com>",
          "timestamp": "2026-02-24T13:50:41+01:00",
          "tree_id": "41b6368b19dbdff4e9b73a5fd3814a889bdd0a70",
          "url": "https://github.com/westermo/GraphX/commit/1750afe8f95c869935a45447f903a4e2bd9be543"
        },
        "date": 1771937733664,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 266905.7016225961,
            "unit": "ns",
            "range": "± 2651.414114038339"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2755464.763541667,
            "unit": "ns",
            "range": "± 41444.89383121529"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 18402142.07142857,
            "unit": "ns",
            "range": "± 228661.145038117"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 539836.4899553572,
            "unit": "ns",
            "range": "± 3811.306016076819"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 6730173.575334822,
            "unit": "ns",
            "range": "± 31860.305367722347"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 61416535.808823526,
            "unit": "ns",
            "range": "± 1227258.8720252584"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 538694.455078125,
            "unit": "ns",
            "range": "± 4380.779454551479"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 6838108.349158654,
            "unit": "ns",
            "range": "± 26252.59087047216"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 59289163.36507935,
            "unit": "ns",
            "range": "± 1909737.482217711"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 18351190.414583333,
            "unit": "ns",
            "range": "± 337447.82282489864"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 60328722.38888889,
            "unit": "ns",
            "range": "± 673915.0501815373"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 60768928.115226336,
            "unit": "ns",
            "range": "± 1650363.5391024426"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 59604388.25,
            "unit": "ns",
            "range": "± 1380301.5722941638"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 59114860.15441176,
            "unit": "ns",
            "range": "± 1145062.7201486903"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 20468090.497916665,
            "unit": "ns",
            "range": "± 278121.2373310449"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 6706679.0703125,
            "unit": "ns",
            "range": "± 55455.2996038245"
          }
        ]
      }
    ]
  }
}