```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8893/24H2/2024Update/HudsonValley)
12th Gen Intel Core i7-1255U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.400-preview.0.26322.102
  [Host]     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3


```
| Method                                                       | Mean     | Error     | StdDev    | Allocated |
|------------------------------------------------------------- |---------:|----------:|----------:|----------:|
| &#39;GraphArea matrix (120V/260E) - warm raster-cache pan frame&#39; | 1.441 ms | 0.0273 ms | 0.0280 ms |   1.96 KB |
