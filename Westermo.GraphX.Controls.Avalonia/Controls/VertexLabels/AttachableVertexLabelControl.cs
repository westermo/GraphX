using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Controls.Models.Interfaces;

namespace Westermo.GraphX.Controls.Controls.VertexLabels;

public class AttachableVertexLabelControl : VertexLabelControl, IAttachableControl<VertexControl>
{
    /// <summary>
    /// Gets label attach node
    /// </summary>
    public VertexControl? AttachNode
    {
        get => GetValue(AttachNodeProperty);
        private set => SetValue(AttachNodeProperty, value);
    }

    public static readonly StyledProperty<VertexControl?> AttachNodeProperty =
        AvaloniaProperty.Register<AttachableVertexLabelControl, VertexControl?>(nameof(AttachNode));


    public AttachableVertexLabelControl()
    {
        DataContext = this;
    }

    /// <summary>
    /// Attach label to VertexControl
    /// </summary>
    /// <param name="node">VertexControl node</param>
    public virtual void Attach(VertexControl node)
    {
        AttachNode = node;
        node.AttachLabel(this);
    }

    /// <summary>
    /// Detach label from control
    /// </summary>
    public virtual void Detach()
    {
        AttachNode = null;
    }

    protected override VertexControl? GetVertexControl(Control? parent)
    {
        return AttachNode;
    }

    public override void UpdatePosition()
    {
        if (double.IsNaN(DesiredSize.Width) || DesiredSize.Width == 0) return;

        var vc = GetVertexControl(GetParent());
        if (vc == null) return;

        if (LabelPositionMode == VertexLabelPositionMode.Sides)
        {
            var vcPos = vc.GetPosition();
            if (double.IsNaN(vcPos.X) || double.IsNaN(vcPos.Y)) return;
            var pt = LabelPositionSide switch
            {
                VertexLabelPositionSide.TopRight => new Point(vcPos.X + vc.DesiredSize.Width,
                    vcPos.Y + -DesiredSize.Height),
                VertexLabelPositionSide.BottomRight => new Point(vcPos.X + vc.DesiredSize.Width,
                    vcPos.Y + vc.DesiredSize.Height),
                VertexLabelPositionSide.TopLeft => new Point(vcPos.X + -DesiredSize.Width,
                    vcPos.Y + -DesiredSize.Height),
                VertexLabelPositionSide.BottomLeft => new Point(vcPos.X + -DesiredSize.Width,
                    vcPos.Y + vc.DesiredSize.Height),
                VertexLabelPositionSide.Top => new Point(
                    vcPos.X + vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, vcPos.Y + -DesiredSize.Height),
                VertexLabelPositionSide.Bottom => new Point(
                    vcPos.X + vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, vcPos.Y + vc.DesiredSize.Height),
                VertexLabelPositionSide.Left => new Point(vcPos.X + -DesiredSize.Width,
                    vcPos.Y + vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                VertexLabelPositionSide.Right => new Point(vcPos.X + vc.DesiredSize.Width,
                    vcPos.Y + vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                _ => throw new GX_InvalidDataException("UpdatePosition() -> Unknown vertex label side!"),
            };
            LastKnownRectSize = new Rect(pt, DesiredSize);
        }
        else LastKnownRectSize = new Rect(LabelPosition, DesiredSize);

        Arrange(LastKnownRectSize);
    }
}