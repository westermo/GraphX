window.BENCHMARK_DATA = {
  "lastUpdate": 1786609366296,
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
          "id": "ece4cdd2868f508a9f3f88618fb3ae398e53074a",
          "message": "Avalonia 12 (#16)",
          "timestamp": "2026-04-13T15:48:09+02:00",
          "tree_id": "fa7a304d8f4826b906560a7038e5e5da529edd14",
          "url": "https://github.com/westermo/GraphX/commit/ece4cdd2868f508a9f3f88618fb3ae398e53074a"
        },
        "date": 1776088448207,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 291125.5967145647,
            "unit": "ns",
            "range": "± 3832.591594988057"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2873463.690848214,
            "unit": "ns",
            "range": "± 65972.23835948946"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 18818231.391666666,
            "unit": "ns",
            "range": "± 164915.47400247853"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 562295.8703264509,
            "unit": "ns",
            "range": "± 4637.0305287625215"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 7244704.046875,
            "unit": "ns",
            "range": "± 70996.94437787523"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 61232391.553571425,
            "unit": "ns",
            "range": "± 441005.28355272836"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 561329.2121233259,
            "unit": "ns",
            "range": "± 6505.679788572231"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 7297842.941666666,
            "unit": "ns",
            "range": "± 63839.91634858622"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 60682395.946428575,
            "unit": "ns",
            "range": "± 618126.4272409745"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_VertexCreationOnly",
            "value": 18992725.352083333,
            "unit": "ns",
            "range": "± 241820.50356972258"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 61613466.701754384,
            "unit": "ns",
            "range": "± 1302739.120348246"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 61220024.483333334,
            "unit": "ns",
            "range": "± 879637.5501319877"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 60547164.78333333,
            "unit": "ns",
            "range": "± 561008.8032291087"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 61951468.3125,
            "unit": "ns",
            "range": "± 875883.9500015796"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 21168916.125,
            "unit": "ns",
            "range": "± 192442.96550094956"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 7187045.212611607,
            "unit": "ns",
            "range": "± 95009.1637455744"
          }
        ]
      },
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
          "id": "ba478e1c9633d3d1e9f666d6d2c9019921e1cb32",
          "message": "Benchmark fixes",
          "timestamp": "2026-04-16T10:19:20+02:00",
          "tree_id": "3f318243582c739fcda899d669b661d62ae3e6b8",
          "url": "https://github.com/westermo/GraphX/commit/ba478e1c9633d3d1e9f666d6d2c9019921e1cb32"
        },
        "date": 1776327898273,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 401308.6,
            "unit": "ns",
            "range": "± 78351.49271952499"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2009694.8,
            "unit": "ns",
            "range": "± 674664.9255099073"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 9839698,
            "unit": "ns",
            "range": "± 2765401.287907055"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 777633.7,
            "unit": "ns",
            "range": "± 250907.3305465807"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5423141.7,
            "unit": "ns",
            "range": "± 1460186.850466295"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 58684647.4,
            "unit": "ns",
            "range": "± 8013988.868206549"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 26933.7,
            "unit": "ns",
            "range": "± 7101.981602185251"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 75670,
            "unit": "ns",
            "range": "± 7887.847685592763"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 321536.2,
            "unit": "ns",
            "range": "± 38074.22740093064"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 21518936.9,
            "unit": "ns",
            "range": "± 3054942.4490867783"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 329040.8,
            "unit": "ns",
            "range": "± 65327.728209900786"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 865296.3,
            "unit": "ns",
            "range": "± 180436.1636553986"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 332618.4,
            "unit": "ns",
            "range": "± 37609.73255008455"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1263830,
            "unit": "ns",
            "range": "± 307721.27994519833"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 78337.7,
            "unit": "ns",
            "range": "± 29996.324278706772"
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
          "id": "5e41f82d9a0f7bcb07176726a3c222fe90c46d20",
          "message": "Some fixes for zoomcontrol (#19)",
          "timestamp": "2026-05-11T09:56:28+02:00",
          "tree_id": "4209190448ca555e60d6784186fcec2b6481848e",
          "url": "https://github.com/westermo/GraphX/commit/5e41f82d9a0f7bcb07176726a3c222fe90c46d20"
        },
        "date": 1778486492762,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 384554.2,
            "unit": "ns",
            "range": "± 96466.40753570356"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2053271.8,
            "unit": "ns",
            "range": "± 602547.4379135454"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 9965555.8,
            "unit": "ns",
            "range": "± 2712609.299301599"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 708595.4,
            "unit": "ns",
            "range": "± 179858.09078468254"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5208917.2,
            "unit": "ns",
            "range": "± 1357054.6698577933"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 56050374.4,
            "unit": "ns",
            "range": "± 3036721.00457488"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 29702.7,
            "unit": "ns",
            "range": "± 8354.011332554226"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 100833.7,
            "unit": "ns",
            "range": "± 12580.456015847227"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 276794.4,
            "unit": "ns",
            "range": "± 19392.39437511521"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 22474059.7,
            "unit": "ns",
            "range": "± 2678371.784773812"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 298123.3,
            "unit": "ns",
            "range": "± 22300.38255302162"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 756006,
            "unit": "ns",
            "range": "± 85455.56912870635"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 263896.1,
            "unit": "ns",
            "range": "± 20520.7318119993"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1220214.7,
            "unit": "ns",
            "range": "± 299227.7243353296"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 59883,
            "unit": "ns",
            "range": "± 9115.829845810955"
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
          "id": "14f28eb25969c6e4d85d723377102e92ec02b0a1",
          "message": "Cleanup some of the edge-rendering math. (#20)\n\n* Add connection point extensions and refactor edge math utilities\n\n* Potential fix for pull request finding\n\nCo-authored-by: Copilot Autofix powered by AI <175728472+Copilot@users.noreply.github.com>\n\n---------\n\nCo-authored-by: Copilot Autofix powered by AI <175728472+Copilot@users.noreply.github.com>",
          "timestamp": "2026-05-11T11:21:26+02:00",
          "tree_id": "5f10ae4228fad5683a7194f1fcdc98c103aa9c53",
          "url": "https://github.com/westermo/GraphX/commit/14f28eb25969c6e4d85d723377102e92ec02b0a1"
        },
        "date": 1778491576159,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 405755.9,
            "unit": "ns",
            "range": "± 78497.15987707241"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2098912.6,
            "unit": "ns",
            "range": "± 555269.4083416126"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 11135070.2,
            "unit": "ns",
            "range": "± 2667764.814746932"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 706963.1,
            "unit": "ns",
            "range": "± 158240.67699653658"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5520521.7,
            "unit": "ns",
            "range": "± 1267273.0204191334"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 54710912.5,
            "unit": "ns",
            "range": "± 3639368.199125357"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 24029.8,
            "unit": "ns",
            "range": "± 5936.225808074053"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 95203.3,
            "unit": "ns",
            "range": "± 18425.246520588116"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 358051.8,
            "unit": "ns",
            "range": "± 52075.81181913751"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 24371976.5,
            "unit": "ns",
            "range": "± 2771855.08255401"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 362219.3,
            "unit": "ns",
            "range": "± 37362.19583735529"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 859460.2,
            "unit": "ns",
            "range": "± 122697.69692559207"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 354682.5,
            "unit": "ns",
            "range": "± 46591.257826263216"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1254142.6,
            "unit": "ns",
            "range": "± 230943.8221483504"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 60142.2,
            "unit": "ns",
            "range": "± 13574.37619438428"
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
          "id": "c3d337f596f1b6b313a389c9671b8d2e7ebb1dad",
          "message": "Avalonia Minimap (#21)\n\n* Return of the WayFinder\n\n* Fix self-loop edge rendering and update desired size calculations\n\n* Dependency updates\n\n* Namespacing and theme fixes",
          "timestamp": "2026-05-12T08:25:53+02:00",
          "tree_id": "bf006282cc8188993acdd208db78929b52c02002",
          "url": "https://github.com/westermo/GraphX/commit/c3d337f596f1b6b313a389c9671b8d2e7ebb1dad"
        },
        "date": 1778567462290,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 401050.5,
            "unit": "ns",
            "range": "± 124485.76442687913"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2222301.1,
            "unit": "ns",
            "range": "± 929699.7518460165"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 10269494.7,
            "unit": "ns",
            "range": "± 2814118.734443285"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 707376.6,
            "unit": "ns",
            "range": "± 165317.35455191764"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5394070.2,
            "unit": "ns",
            "range": "± 1376249.5234083582"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 57219253.8,
            "unit": "ns",
            "range": "± 4677489.967898733"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 30717.3,
            "unit": "ns",
            "range": "± 6837.4957737139075"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 99640.9,
            "unit": "ns",
            "range": "± 11891.661088062228"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 309972.6,
            "unit": "ns",
            "range": "± 20436.862822959018"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 23111757.9,
            "unit": "ns",
            "range": "± 2410545.1246233108"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 271143.4,
            "unit": "ns",
            "range": "± 18420.69042137129"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 826465.3,
            "unit": "ns",
            "range": "± 217724.25716592892"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 352097.8,
            "unit": "ns",
            "range": "± 77583.89098088168"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1197086.5,
            "unit": "ns",
            "range": "± 271027.5194642501"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 73855,
            "unit": "ns",
            "range": "± 17836.06704280839"
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
          "id": "7e456b47fea0bc23fdda63e29187beef9533ab69",
          "message": "Fix Printing and edge removal (#24)",
          "timestamp": "2026-06-12T08:56:04+02:00",
          "tree_id": "6db82a9558debe11f1111bf83bbca307dce8f007",
          "url": "https://github.com/westermo/GraphX/commit/7e456b47fea0bc23fdda63e29187beef9533ab69"
        },
        "date": 1781247658613,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 418600.2,
            "unit": "ns",
            "range": "± 136436.75306390953"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 1924619.7,
            "unit": "ns",
            "range": "± 619937.8626315973"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 10348999,
            "unit": "ns",
            "range": "± 3110676.0619056933"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 712873.2,
            "unit": "ns",
            "range": "± 192905.69804544395"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5543954.9,
            "unit": "ns",
            "range": "± 2229338.0457544615"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 52264050.3,
            "unit": "ns",
            "range": "± 5461149.954222667"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 30456.9,
            "unit": "ns",
            "range": "± 6994.812831266068"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 80646.1,
            "unit": "ns",
            "range": "± 16082.229602542331"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 276023.7,
            "unit": "ns",
            "range": "± 20570.852756320586"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 22623802.5,
            "unit": "ns",
            "range": "± 3654481.3850551355"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 310129,
            "unit": "ns",
            "range": "± 30008.149944825174"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 803764.4,
            "unit": "ns",
            "range": "± 143412.19107577833"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 342804,
            "unit": "ns",
            "range": "± 111514.62624845825"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1240726,
            "unit": "ns",
            "range": "± 299728.4416326812"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 73097.6,
            "unit": "ns",
            "range": "± 18386.008383188197"
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
          "id": "a7aa83a8f05e618bbf2ba73a0e949fbe1cc905a0",
          "message": "Reworked Drag Behavior to be default on shift (#25)\n\n* Reworked Drag Behavior to be default on shift\n\n* Move GetSnapModifier into the if-statement\n\n* Potential fix for pull request finding\n\nCo-authored-by: Copilot Autofix powered by AI <175728472+Copilot@users.noreply.github.com>\n\n* Potential fix for pull request finding\n\nCo-authored-by: Copilot Autofix powered by AI <175728472+Copilot@users.noreply.github.com>\n\n* Safety fix\n\n---------\n\nCo-authored-by: Copilot Autofix powered by AI <175728472+Copilot@users.noreply.github.com>",
          "timestamp": "2026-06-12T10:31:26+02:00",
          "tree_id": "9ecbc8ba4e61e673083e020125832d76713186a1",
          "url": "https://github.com/westermo/GraphX/commit/a7aa83a8f05e618bbf2ba73a0e949fbe1cc905a0"
        },
        "date": 1781253394056,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 453909.7,
            "unit": "ns",
            "range": "± 86958.7426133796"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2126083.8,
            "unit": "ns",
            "range": "± 545131.8689790776"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 11233124.6,
            "unit": "ns",
            "range": "± 2841762.8210335374"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 711462.6,
            "unit": "ns",
            "range": "± 163233.93783442627"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5536953.4,
            "unit": "ns",
            "range": "± 1412293.5634573838"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 59128164.2,
            "unit": "ns",
            "range": "± 4624171.0766904615"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 33614.3,
            "unit": "ns",
            "range": "± 13014.224227103716"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 108582.7,
            "unit": "ns",
            "range": "± 16682.8170752624"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 453707.9,
            "unit": "ns",
            "range": "± 100335.81896749426"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 24689974.6,
            "unit": "ns",
            "range": "± 3713924.4831920858"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 386780.7,
            "unit": "ns",
            "range": "± 61787.93819194091"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 886127,
            "unit": "ns",
            "range": "± 89697.0056257299"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 404264,
            "unit": "ns",
            "range": "± 116409.7165131273"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1311448.4,
            "unit": "ns",
            "range": "± 270079.07415265543"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 69794,
            "unit": "ns",
            "range": "± 11723.937288963787"
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
          "id": "9c4483b6c39128e940d806b38e2808d691568c2e",
          "message": "Fixed print clipping by properly adjuisting for offset (#26)",
          "timestamp": "2026-06-15T09:03:39+02:00",
          "tree_id": "486fba665130cbabc39116ffe9c4f22d4b358f6c",
          "url": "https://github.com/westermo/GraphX/commit/9c4483b6c39128e940d806b38e2808d691568c2e"
        },
        "date": 1781507287491,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 358681.3,
            "unit": "ns",
            "range": "± 95024.64697592937"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 1643086.9,
            "unit": "ns",
            "range": "± 612800.3988427861"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 8538050.4,
            "unit": "ns",
            "range": "± 2849586.6187012773"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 662085.5,
            "unit": "ns",
            "range": "± 201620.89220793123"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 4469285.3,
            "unit": "ns",
            "range": "± 1144492.4955167829"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 38920900.9,
            "unit": "ns",
            "range": "± 3395667.3100487744"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 27280.4,
            "unit": "ns",
            "range": "± 7845.146726196748"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 75502,
            "unit": "ns",
            "range": "± 9888.976286754863"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 291956,
            "unit": "ns",
            "range": "± 26746.725049786728"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 18821961.7,
            "unit": "ns",
            "range": "± 2382399.922693177"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 306760.2,
            "unit": "ns",
            "range": "± 56216.09160452983"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 766389.3,
            "unit": "ns",
            "range": "± 150245.45857070762"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 275463.9,
            "unit": "ns",
            "range": "± 38679.52848859459"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1056167.3,
            "unit": "ns",
            "range": "± 267829.4534023263"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 66488.5,
            "unit": "ns",
            "range": "± 18322.311088154547"
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
          "id": "55d76014df3ce5a1644a7b0c7eeffa76ebc5b875",
          "message": "Fixed edge arrange when vertices are overlapping (#27)",
          "timestamp": "2026-06-16T15:02:59+02:00",
          "tree_id": "b3d04fe63344ca677aedaaf17864d26c34fb560d",
          "url": "https://github.com/westermo/GraphX/commit/55d76014df3ce5a1644a7b0c7eeffa76ebc5b875"
        },
        "date": 1781615265600,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 384154.7,
            "unit": "ns",
            "range": "± 77742.58093370282"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2164698.8,
            "unit": "ns",
            "range": "± 617606.2712627412"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 10668576,
            "unit": "ns",
            "range": "± 2682710.6423059413"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 701840.9,
            "unit": "ns",
            "range": "± 218421.86926651318"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5263262.6,
            "unit": "ns",
            "range": "± 1257881.4248524029"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 53783201.6,
            "unit": "ns",
            "range": "± 6086328.98026646"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 24478.7,
            "unit": "ns",
            "range": "± 6489.513011175971"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 79939.5,
            "unit": "ns",
            "range": "± 11118.428298700015"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 401805.4,
            "unit": "ns",
            "range": "± 111430.21227875717"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 23546143.9,
            "unit": "ns",
            "range": "± 3202884.3606996713"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 361312.7,
            "unit": "ns",
            "range": "± 60881.344979029134"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 786675.4,
            "unit": "ns",
            "range": "± 109201.701674979"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 352015.5,
            "unit": "ns",
            "range": "± 72896.25347063836"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1200446.6,
            "unit": "ns",
            "range": "± 203419.9281984601"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 60294.6,
            "unit": "ns",
            "range": "± 11297.796070227345"
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
          "id": "4ff3c1b549c14bed4d5cff2ffb6f0d47980f9108",
          "message": "Fixes for WayFinder and ZoomControl mouse interactions (#28)",
          "timestamp": "2026-06-16T15:10:34+02:00",
          "tree_id": "b0075eec3477d1ffdd9b797c39f6ad7f534aa9e5",
          "url": "https://github.com/westermo/GraphX/commit/4ff3c1b549c14bed4d5cff2ffb6f0d47980f9108"
        },
        "date": 1781615727735,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 419454.2,
            "unit": "ns",
            "range": "± 90464.90664438768"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2188374.1,
            "unit": "ns",
            "range": "± 505536.23564542277"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 11380794.7,
            "unit": "ns",
            "range": "± 2758328.7233995735"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 788198.1,
            "unit": "ns",
            "range": "± 217157.75345098268"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5549465.5,
            "unit": "ns",
            "range": "± 1283774.3537588466"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 52976009.2,
            "unit": "ns",
            "range": "± 4298208.018108446"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 26309.4,
            "unit": "ns",
            "range": "± 6621.949430995881"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 86192.2,
            "unit": "ns",
            "range": "± 14650.053718521156"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 360861.3,
            "unit": "ns",
            "range": "± 49322.2149792115"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 23755197.1,
            "unit": "ns",
            "range": "± 3543085.621714511"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 407758.1,
            "unit": "ns",
            "range": "± 53605.99953663976"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 868873.1,
            "unit": "ns",
            "range": "± 146405.7375237657"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 327866.7,
            "unit": "ns",
            "range": "± 65524.20135263001"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1360944.2,
            "unit": "ns",
            "range": "± 256525.74589342446"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 65188,
            "unit": "ns",
            "range": "± 8555.693140307867"
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
          "id": "977ff22334ad8deb25739da2c9b1f11c1827045f",
          "message": "Fix mouse shortcuts (#29)\n\n* Fixes for WayFinder and ZoomControl mouse interactions\n\n* fix: middle-button pan and Ctrl+Alt click selection in ZoomControl\n\n- Add middle mouse button drag to initiate pan mode, enabling scroll\n  wheel drag to pan the viewbox.\n- Skip firing AreaSelected when ZoomBox has zero area (click without\n  drag). Previously, Ctrl+Alt clicking a device would momentarily\n  select it on pointer-down but then clear the selection on pointer-up\n  because CompleteInteraction fired AreaSelected with an empty rect.\n\nCo-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>\n\n---------\n\nCo-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>",
          "timestamp": "2026-07-27T15:32:19+02:00",
          "tree_id": "a6827b550955facc94fab9c581ae13c9a0b9ca9f",
          "url": "https://github.com/westermo/GraphX/commit/977ff22334ad8deb25739da2c9b1f11c1827045f"
        },
        "date": 1785159419418,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 370903.5,
            "unit": "ns",
            "range": "± 73722.36611360822"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2137014.7,
            "unit": "ns",
            "range": "± 516011.9685033263"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 10798260.4,
            "unit": "ns",
            "range": "± 2641416.538665452"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 695574.6,
            "unit": "ns",
            "range": "± 156669.54488717823"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5184303.2,
            "unit": "ns",
            "range": "± 1230474.038617439"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 51380498.8,
            "unit": "ns",
            "range": "± 4117703.1768701347"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 23012.1,
            "unit": "ns",
            "range": "± 6134.686579877844"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 84827,
            "unit": "ns",
            "range": "± 11352.545627401028"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 335445.2,
            "unit": "ns",
            "range": "± 42394.70004034834"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 24124780.2,
            "unit": "ns",
            "range": "± 3010349.1003667773"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 342205.1,
            "unit": "ns",
            "range": "± 60998.38766803595"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 780623.5,
            "unit": "ns",
            "range": "± 64595.54540583876"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 349026.7,
            "unit": "ns",
            "range": "± 85328.47873046581"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1244930.1,
            "unit": "ns",
            "range": "± 246358.49659589355"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 83211.2,
            "unit": "ns",
            "range": "± 18742.515334572003"
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
          "id": "bc9db5984c39823cdbfea1d4ba6804bc8eff42fd",
          "message": "Performance Optimizations (#30)\n\n* Many optimizations\n\n* Delete BenchmarkDotNet.Artifacts/results/GraphXBenchmarks.GraphComponentRenderBenchmarks-report-full-compressed.json\n\n* Delete BenchmarkDotNet.Artifacts/results/GraphXBenchmarks.GraphComponentRenderBenchmarks-report-full.json\n\n* Delete BenchmarkDotNet.Artifacts/results/GraphXBenchmarks.GraphComponentRenderBenchmarks-report-github.md\n\n* Delete BenchmarkDotNet.Artifacts/results/GraphXBenchmarks.GraphComponentRenderBenchmarks-report.csv\n\n* Delete BenchmarkDotNet.Artifacts/results/GraphXBenchmarks.GraphComponentRenderBenchmarks-report.html\n\n* small fixes",
          "timestamp": "2026-08-13T09:36:42+02:00",
          "tree_id": "5ab80058291779e6992612849b004b021e47ba22",
          "url": "https://github.com/westermo/GraphX/commit/bc9db5984c39823cdbfea1d4ba6804bc8eff42fd"
        },
        "date": 1786609362202,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadVertexes",
            "value": 371182.3,
            "unit": "ns",
            "range": "± 85965.05554784714"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadVertexes",
            "value": 2261213.4,
            "unit": "ns",
            "range": "± 611702.7414072587"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadVertexes",
            "value": 11232431.3,
            "unit": "ns",
            "range": "± 2680730.8982028286"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_PreloadAndGenerateEdges",
            "value": 725864.6,
            "unit": "ns",
            "range": "± 180042.04597272395"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_PreloadAndGenerateEdges",
            "value": 5040952.9,
            "unit": "ns",
            "range": "± 1179750.065487832"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PreloadAndGenerateEdges",
            "value": 53770124.1,
            "unit": "ns",
            "range": "± 8011123.922822876"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.SmallGraph_UpdateAllEdges",
            "value": 22776.5,
            "unit": "ns",
            "range": "± 5832.301913576758"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_UpdateAllEdges",
            "value": 88918.3,
            "unit": "ns",
            "range": "± 11626.87066955966"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateAllEdges",
            "value": 377361.6,
            "unit": "ns",
            "range": "± 41294.757251846015"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_EdgeGenerationOnly",
            "value": 23619447,
            "unit": "ns",
            "range": "± 3828192.333285806"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdgesRenderingOnly",
            "value": 349071.3,
            "unit": "ns",
            "range": "± 51328.903681281"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithParallelEdges",
            "value": 882771.6,
            "unit": "ns",
            "range": "± 158810.66842851305"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_UpdateEdges_WithCurving",
            "value": 378668.9,
            "unit": "ns",
            "range": "± 53057.97283988314"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.LargeGraph_PositionUpdatesCost",
            "value": 1299835.2,
            "unit": "ns",
            "range": "± 231429.7753919222"
          },
          {
            "name": "GraphXBenchmarks.GraphRenderingBenchmarks.MediumGraph_WithSelfLoops_UpdateAllEdges",
            "value": 79849.5,
            "unit": "ns",
            "range": "± 10868.588002434662"
          }
        ]
      }
    ]
  }
}