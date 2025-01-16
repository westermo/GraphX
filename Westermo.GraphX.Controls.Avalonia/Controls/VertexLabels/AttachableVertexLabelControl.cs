

using System.ComponentModel;
using Westermo.GraphX.Controls.Models;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class AttachableVertexLabelControl : VertexLabelControl, IAttachableControl<VertexControl>, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets label attach node
        /// </summary>
        public VertexControl? AttachNode { get => (VertexControl)GetValue(AttachNodeProperty);
            private set { SetValue(AttachNodeProperty, value); OnPropertyChanged("AttachNode"); } }

        public static readonly StyledProperty<> AttachNodeProperty = AvaloniaProperty.Register(nameof(AttachNode), typeof(VertexControl), typeof(AttachableVertexLabelControl), 
            new PropertyMetadata(null));

        static AttachableVertexLabelControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AttachableVertexLabelControl), new FrameworkPropertyMetadata(typeof(AttachableVertexLabelControl)));
        }

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
            if (AttachNode != null)
                AttachNode.IsVisibleChanged -= AttachNode_IsVisibleChanged;
            AttachNode = node;

            AttachNode.IsVisibleChanged += AttachNode_IsVisibleChanged;
            node.AttachLabel(this);
        }

        /// <summary>
        /// Detach label from control
        /// </summary>
        public virtual void Detach()
        {
            if (AttachNode != null)
                AttachNode.IsVisibleChanged -= AttachNode_IsVisibleChanged;
            AttachNode = null;
        }

        private void AttachNode_IsVisibleChanged(object sender, StyledPropertyChangedEventArgs e)
        {
            if (AttachNode!.IsVisible && AttachNode.ShowLabel)
                Show();
            else if (!AttachNode.IsVisible)
            {
                Hide();
            }
        }

        protected override VertexControl? GetVertexControl(Control? parent)
        {
            //if(AttachNode == null)
            //    throw new GX_InvalidDataException("AttachableVertexLabelControl node is not attached!");
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
                var pt = LabelPositionSide switch
                {
                    VertexLabelPositionSide.TopRight => new Point(vcPos.X + vc.DesiredSize.Width, vcPos.Y + -DesiredSize.Height),
                    VertexLabelPositionSide.BottomRight => new Point(vcPos.X + vc.DesiredSize.Width, vcPos.Y + vc.DesiredSize.Height),
                    VertexLabelPositionSide.TopLeft => new Point(vcPos.X + -DesiredSize.Width, vcPos.Y + -DesiredSize.Height),
                    VertexLabelPositionSide.BottomLeft => new Point(vcPos.X + -DesiredSize.Width, vcPos.Y + vc.DesiredSize.Height),
                    VertexLabelPositionSide.Top => new Point(vcPos.X + vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, vcPos.Y + -DesiredSize.Height),
                    VertexLabelPositionSide.Bottom => new Point(vcPos.X + vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, vcPos.Y + vc.DesiredSize.Height),
                    VertexLabelPositionSide.Left => new Point(vcPos.X + -DesiredSize.Width, vcPos.Y + vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                    VertexLabelPositionSide.Right => new Point(vcPos.X + vc.DesiredSize.Width, vcPos.Y + vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                    _ => throw new GX_InvalidDataException("UpdatePosition() -> Unknown vertex label side!"),
                };
                LastKnownRectSize = new Rect(pt, DesiredSize);
            }
            else LastKnownRectSize = new Rect(LabelPosition, DesiredSize);

            Arrange(LastKnownRectSize);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
