using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.ViewModels;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;

namespace ShowcaseApp.Avalonia.Pages;

/// <summary>
/// GeneralGraph page demonstrating MVVM pattern with GraphX.
/// The code-behind is minimal - only used for view-specific operations
/// that cannot be done in XAML or the ViewModel.
/// </summary>
public partial class GeneralGraph : UserControl
{
    private readonly GeneralGraphViewModel? _viewModel;

    public GeneralGraph()
    {
        InitializeComponent();

        // Create and set the ViewModel
        _viewModel = new GeneralGraphViewModel();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // Pass the GraphArea reference to the ViewModel for operations that require it
        _viewModel.SetGraphArea(graphArea);

        // Subscribe to ViewModel events for view-specific operations
        _viewModel.RelayoutFinished += OnRelayoutFinished;
        _viewModel.GenerateGraphFinished += OnGenerateGraphFinished;
        _viewModel.SaveLayoutRequested += OnSaveLayoutRequested;
        _viewModel.LoadLayoutRequested += OnLoadLayoutRequested;
        _viewModel.ExportAsImageRequested += OnExportAsImageRequested;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // Unsubscribe from events
        _viewModel.RelayoutFinished -= OnRelayoutFinished;
        _viewModel.GenerateGraphFinished -= OnGenerateGraphFinished;
        _viewModel.SaveLayoutRequested -= OnSaveLayoutRequested;
        _viewModel.LoadLayoutRequested -= OnLoadLayoutRequested;
        _viewModel.ExportAsImageRequested -= OnExportAsImageRequested;
    }

    /// <summary>
    /// Handles zoom to fill after relayout - this is view-specific behavior.
    /// </summary>
    private void OnRelayoutFinished(object? sender, EventArgs e)
    {
        zoomCtrl.ZoomToFill();
        zoomCtrl.Mode = ZoomControlModes.Custom;
    }

    /// <summary>
    /// Handles zoom to fill after graph generation - this is view-specific behavior.
    /// </summary>
    private void OnGenerateGraphFinished(object? sender, EventArgs e)
    {
        zoomCtrl.ZoomToFill();
        zoomCtrl.Mode = ZoomControlModes.Custom;
    }

    /// <summary>
    /// Handles save layout request by showing a file save dialog.
    /// Uses JSON serialization which is AOT-compatible.
    /// </summary>
    private async void OnSaveLayoutRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Layout",
            SuggestedFileName = "layout.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                var data = graphArea.ExtractSerializationData();

                // Convert to a portable format that includes type information
                var portableData = ConvertToPortableFormat(data);
                await JsonSerializer.SerializeAsync(stream, portableData,
                    LayoutSerializationContext.Default.ListPortableSerializationData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save layout: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles load layout request by showing a file open dialog.
    /// Uses JSON deserialization which is AOT-compatible.
    /// </summary>
    private async void OnLoadLayoutRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Layout",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            try
            {
                await using var stream = await files[0].OpenReadAsync();
                var portableData = await JsonSerializer.DeserializeAsync(
                    stream,
                    LayoutSerializationContext.Default.ListPortableSerializationData);

                if (portableData != null)
                {
                    var data = ConvertFromPortableFormat(portableData);
                    graphArea.RebuildFromSerializationData(data);
                    graphArea.SetVerticesDrag(true, true);
                    graphArea.UpdateAllEdges();
                    zoomCtrl.ZoomToFill();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load layout: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles export as image request.
    /// </summary>
    private async void OnExportAsImageRequested(object? sender, EventArgs e)
    {
        await graphArea.ExportAsImageDialog(ImageType.PNG, true);
    }

    #region JSON Serialization Helpers

    /// <summary>
    /// Converts GraphSerializationData list to a portable format for JSON serialization.
    /// </summary>
    private static List<PortableSerializationData> ConvertToPortableFormat(List<GraphSerializationData> data)
    {
        var result = new List<PortableSerializationData>();
        foreach (var item in data)
        {
            var portable = new PortableSerializationData
            {
                PositionX = item.Position.X,
                PositionY = item.Position.Y,
                IsVisible = item.IsVisible,
                HasLabel = item.HasLabel
            };

            // Serialize based on the actual type of Data
            if (item.Data is DataVertex vertex)
            {
                portable.DataType = "Vertex";
                portable.VertexData = new PortableVertexData
                {
                    Id = vertex.ID,
                    Text = vertex.Text,
                    Name = vertex.Name,
                    Profession = vertex.Profession,
                    Gender = vertex.Gender,
                    Age = vertex.Age,
                    ImageId = vertex.ImageId,
                    IsBlue = vertex.IsBlue
                };
            }
            else if (item.Data is DataEdge edge)
            {
                portable.DataType = "Edge";
                portable.EdgeData = new PortableEdgeData
                {
                    Id = edge.ID,
                    SourceId = edge.Source?.ID ?? 0,
                    TargetId = edge.Target?.ID ?? 0,
                    Weight = edge.Weight,
                    Text = edge.Text,
                    ToolTipText = edge.ToolTipText,
                    ArrowTarget = edge.ArrowTarget,
                    Angle = edge.Angle
                };
            }

            result.Add(portable);
        }

        return result;
    }

    /// <summary>
    /// Converts portable format back to GraphSerializationData list.
    /// </summary>
    private static List<GraphSerializationData> ConvertFromPortableFormat(List<PortableSerializationData> portableData)
    {
        var result = new List<GraphSerializationData>();

        // First pass: create all vertices and build a lookup dictionary
        var vertexLookup = new Dictionary<long, DataVertex>();
        foreach (var item in portableData)
        {
            if (item is { DataType: "Vertex", VertexData: not null })
            {
                var vertex = new DataVertex(item.VertexData.Text)
                {
                    ID = item.VertexData.Id,
                    Name = item.VertexData.Name,
                    Profession = item.VertexData.Profession,
                    Gender = item.VertexData.Gender,
                    Age = item.VertexData.Age,
                    ImageId = item.VertexData.ImageId,
                    IsBlue = item.VertexData.IsBlue
                };
                vertexLookup[vertex.ID] = vertex;

                result.Add(new GraphSerializationData
                {
                    Data = vertex,
                    Position = new Westermo.GraphX.Measure.Point(item.PositionX, item.PositionY),
                    IsVisible = item.IsVisible,
                    HasLabel = item.HasLabel
                });
            }
        }

        // Second pass: create edges with proper vertex references
        foreach (var item in portableData)
        {
            if (item is { DataType: "Edge", EdgeData: not null })
            {
                // Get source and target vertices from lookup
                vertexLookup.TryGetValue(item.EdgeData.SourceId, out var source);
                vertexLookup.TryGetValue(item.EdgeData.TargetId, out var target);

                if (source != null && target != null)
                {
                    var edge = new DataEdge(source, target, item.EdgeData.Weight)
                    {
                        ID = item.EdgeData.Id,
                        Text = item.EdgeData.Text,
                        ToolTipText = item.EdgeData.ToolTipText,
                        ArrowTarget = item.EdgeData.ArrowTarget,
                        Angle = item.EdgeData.Angle
                    };

                    result.Add(new GraphSerializationData
                    {
                        Data = edge,
                        Position = new Westermo.GraphX.Measure.Point(item.PositionX, item.PositionY),
                        IsVisible = item.IsVisible,
                        HasLabel = item.HasLabel
                    });
                }
            }
        }

        return result;
    }

    #endregion

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        graphArea.ShowAllEdgesArrows((sender as ToggleButton)?.IsChecked ?? false);
    }
}

#region Portable Serialization Classes

/// <summary>
/// AOT-compatible serialization data that avoids polymorphic object references.
/// </summary>
public class PortableSerializationData
{
    /// <summary>
    /// Type discriminator: "Vertex" or "Edge"
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate of the control position
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Y coordinate of the control position
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// Control visibility
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Control label availability
    /// </summary>
    public bool HasLabel { get; set; }

    /// <summary>
    /// Vertex data (populated when DataType is "Vertex")
    /// </summary>
    public PortableVertexData? VertexData { get; set; }

    /// <summary>
    /// Edge data (populated when DataType is "Edge")
    /// </summary>
    public PortableEdgeData? EdgeData { get; set; }
}

/// <summary>
/// Serializable vertex data.
/// </summary>
public class PortableVertexData
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Profession { get; set; }
    public string? Gender { get; set; }
    public int Age { get; set; }
    public int ImageId { get; set; }
    public bool IsBlue { get; set; }
}

/// <summary>
/// Serializable edge data.
/// </summary>
public class PortableEdgeData
{
    public long Id { get; set; }
    public long SourceId { get; set; }
    public long TargetId { get; set; }
    public double Weight { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ToolTipText { get; set; } = string.Empty;
    public bool ArrowTarget { get; set; }
    public double Angle { get; set; }
}

#endregion

#region JSON Serialization Context

/// <summary>
/// Source-generated JSON serialization context for AOT compatibility.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<PortableSerializationData>))]
[JsonSerializable(typeof(PortableSerializationData))]
[JsonSerializable(typeof(PortableVertexData))]
[JsonSerializable(typeof(PortableEdgeData))]
internal partial class LayoutSerializationContext : JsonSerializerContext
{
}

#endregion