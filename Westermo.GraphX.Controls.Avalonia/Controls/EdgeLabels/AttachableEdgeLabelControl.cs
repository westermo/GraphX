using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Controls.Models.Interfaces;

namespace Westermo.GraphX.Controls.Controls.EdgeLabels;

public class AttachableEdgeLabelControl : EdgeLabelControl, IAttachableControl<EdgeControl>
{
    /// <summary>
    /// Gets label attach node
    /// </summary>
    public EdgeControl? AttachNode
    {
        get => GetValue(AttachNodeProperty);
        private set => SetValue(AttachNodeProperty, value);
    }

    public static readonly StyledProperty<EdgeControl?> AttachNodeProperty =
        AvaloniaProperty.Register<AttachableEdgeLabelControl, EdgeControl?>(nameof(AttachNode));

    public AttachableEdgeLabelControl()
    {
        DataContext = this;
    }

    /// <summary>
    /// Attach label to EdgeControl
    /// </summary>
    /// <param name="node">EdgeControl node</param>
    public virtual void Attach(EdgeControl node)
    {
        if (AttachNode != null)
            AttachNode.PropertyChanged -= AttachNode_IsVisibleChanged;
        AttachNode = node;
        AttachNode.PropertyChanged += AttachNode_IsVisibleChanged;
        node.AttachLabel(this);
    }

    /// <summary>
    /// Detach label from control
    /// </summary>
    public virtual void Detach()
    {
        if (AttachNode != null)
            AttachNode.PropertyChanged -= AttachNode_IsVisibleChanged;
        AttachNode = null;
    }

    private void AttachNode_IsVisibleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != IsVisibleProperty) return;
        if (AttachNode!.IsVisible && ShowLabel)
            Show();
        else if (!AttachNode.IsVisible)
            Hide();
    }


    protected override EdgeControl GetEdgeControl(Control? parent)
    {
        return AttachNode ?? throw new GX_InvalidDataException("AttachableEdgeLabelControl node is not attached!");
    }
}