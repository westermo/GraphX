using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Direct unit coverage for the pixel-size/memory-budget validation shared by the GraphArea raster cache
/// and the Wayfinder minimap content cache (<see cref="RasterCacheGeometry"/>). The end-to-end behavior of
/// each caller is covered by <c>GraphAreaRasterCacheTests</c> and <c>WayfinderRasterCacheTests</c>; these
/// tests exercise the shared validation logic itself in isolation.
/// </summary>
public sealed class RasterCacheGeometryTests
{
    [Test]
    public async Task TryGetPixelSize_ComputesScaledPixelSize_ForValidInput()
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, 100, 50), renderScaling: 2, maximumBytes: 64L * 1024 * 1024,
            maximumDimension: 8192, out var pixelSize);

        await Assert.That(success).IsTrue();
        await Assert.That(pixelSize).IsEqualTo(new PixelSize(200, 100));
    }

    [Test]
    [Arguments(0, 100)]
    [Arguments(100, 0)]
    [Arguments(-1, 100)]
    public async Task TryGetPixelSize_Fails_ForNonPositiveContentSize(double width, double height)
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, width, height), renderScaling: 1, maximumBytes: 64L * 1024 * 1024,
            maximumDimension: 8192, out _);

        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryGetPixelSize_Fails_WhenScaledDimensionExceedsMaximum()
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, 5000, 100), renderScaling: 2, maximumBytes: long.MaxValue,
            maximumDimension: 8192, out _);

        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryGetPixelSize_Fails_WhenPixelCountExceedsMemoryBudget()
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, 4000, 4000), renderScaling: 1, maximumBytes: 1024,
            maximumDimension: 8192, out _);

        await Assert.That(success).IsFalse();
    }

    [Test]
    [Arguments(double.NaN)]
    [Arguments(0d)]
    [Arguments(-1d)]
    public async Task TryGetPixelSize_Fails_ForNonPositiveRenderScaling(double renderScaling)
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, 100, 50), renderScaling, maximumBytes: 64L * 1024 * 1024,
            maximumDimension: 8192, out _);

        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryGetPixelSize_Fails_WhenMaximumBytesIsNotPositive()
    {
        var success = RasterCacheGeometry.TryGetPixelSize(
            new Rect(0, 0, 100, 50), renderScaling: 1, maximumBytes: 0,
            maximumDimension: 8192, out _);

        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task GetEffectiveRenderScaling_FallsBackToOne_WhenNoTopLevelIsAvailable()
    {
        var control = new Control();

        var scaling = RasterCacheGeometry.GetEffectiveRenderScaling(null, control);

        await Assert.That(scaling).IsEqualTo(1d);
    }
}
