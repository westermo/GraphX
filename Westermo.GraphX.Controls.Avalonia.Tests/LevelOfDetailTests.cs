using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for Level-of-Detail (LOD) settings used for zoom-based visual simplification.
/// </summary>
public class LevelOfDetailTests
{
    [Test]
    public async Task LodSettings_DefaultsAreReasonable()
    {
        var settings = new LevelOfDetailSettings();
        
        await Assert.That(settings.IsEnabled).IsTrue();
        await Assert.That(settings.HideArrowsZoomThreshold).IsGreaterThan(0);
        await Assert.That(settings.HideEdgeLabelsZoomThreshold).IsGreaterThan(0);
        await Assert.That(settings.HideVertexLabelsZoomThreshold).IsGreaterThan(0);
    }

    [Test]
    public async Task LodSettings_ShouldShowArrows_WhenZoomAboveThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.3
        };
        
        await Assert.That(settings.ShouldShowArrows(0.5)).IsTrue();
        await Assert.That(settings.ShouldShowArrows(1.0)).IsTrue();
        await Assert.That(settings.ShouldShowArrows(2.0)).IsTrue();
    }

    [Test]
    public async Task LodSettings_ShouldHideArrows_WhenZoomBelowThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.3
        };
        
        await Assert.That(settings.ShouldShowArrows(0.2)).IsFalse();
        await Assert.That(settings.ShouldShowArrows(0.1)).IsFalse();
        await Assert.That(settings.ShouldShowArrows(0.05)).IsFalse();
    }

    [Test]
    public async Task LodSettings_ShouldShowArrows_WhenLodDisabled()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = false,
            HideArrowsZoomThreshold = 0.3
        };
        
        // Should always show when LOD is disabled, regardless of zoom
        await Assert.That(settings.ShouldShowArrows(0.1)).IsTrue();
        await Assert.That(settings.ShouldShowArrows(0.01)).IsTrue();
    }

    [Test]
    public async Task LodSettings_ShouldShowEdgeLabels_WhenZoomAboveThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideEdgeLabelsZoomThreshold = 0.5
        };
        
        await Assert.That(settings.ShouldShowEdgeLabels(0.6)).IsTrue();
        await Assert.That(settings.ShouldShowEdgeLabels(1.0)).IsTrue();
    }

    [Test]
    public async Task LodSettings_ShouldHideEdgeLabels_WhenZoomBelowThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideEdgeLabelsZoomThreshold = 0.5
        };
        
        await Assert.That(settings.ShouldShowEdgeLabels(0.4)).IsFalse();
        await Assert.That(settings.ShouldShowEdgeLabels(0.1)).IsFalse();
    }

    [Test]
    public async Task LodSettings_ShouldShowVertexLabels_WhenZoomAboveThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideVertexLabelsZoomThreshold = 0.4
        };
        
        await Assert.That(settings.ShouldShowVertexLabels(0.5)).IsTrue();
        await Assert.That(settings.ShouldShowVertexLabels(1.0)).IsTrue();
    }

    [Test]
    public async Task LodSettings_ShouldHideVertexLabels_WhenZoomBelowThreshold()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideVertexLabelsZoomThreshold = 0.4
        };
        
        await Assert.That(settings.ShouldShowVertexLabels(0.3)).IsFalse();
        await Assert.That(settings.ShouldShowVertexLabels(0.1)).IsFalse();
    }

    [Test]
    public async Task LodSettings_AtExactThreshold_ShowsElement()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.3,
            HideEdgeLabelsZoomThreshold = 0.5,
            HideVertexLabelsZoomThreshold = 0.4
        };
        
        // At exact threshold, should show (>= comparison)
        await Assert.That(settings.ShouldShowArrows(0.3)).IsTrue();
        await Assert.That(settings.ShouldShowEdgeLabels(0.5)).IsTrue();
        await Assert.That(settings.ShouldShowVertexLabels(0.4)).IsTrue();
    }

    [Test]
    public async Task LodSettings_ThresholdsCanBeCustomized()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.1,
            HideEdgeLabelsZoomThreshold = 0.2,
            HideVertexLabelsZoomThreshold = 0.15
        };
        
        await Assert.That(settings.HideArrowsZoomThreshold).IsEqualTo(0.1);
        await Assert.That(settings.HideEdgeLabelsZoomThreshold).IsEqualTo(0.2);
        await Assert.That(settings.HideVertexLabelsZoomThreshold).IsEqualTo(0.15);
    }

    [Test]
    public async Task LodSettings_DifferentElementsHaveIndependentThresholds()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.2,
            HideEdgeLabelsZoomThreshold = 0.6,
            HideVertexLabelsZoomThreshold = 0.4
        };
        
        // At zoom 0.3: arrows visible, edge labels hidden, vertex labels hidden
        await Assert.That(settings.ShouldShowArrows(0.3)).IsTrue();
        await Assert.That(settings.ShouldShowEdgeLabels(0.3)).IsFalse();
        await Assert.That(settings.ShouldShowVertexLabels(0.3)).IsFalse();
        
        // At zoom 0.5: arrows visible, edge labels hidden, vertex labels visible
        await Assert.That(settings.ShouldShowArrows(0.5)).IsTrue();
        await Assert.That(settings.ShouldShowEdgeLabels(0.5)).IsFalse();
        await Assert.That(settings.ShouldShowVertexLabels(0.5)).IsTrue();
    }
}
