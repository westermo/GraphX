using Westermo.GraphX.Controls.Models;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class AttachableEdgeLabelControl : EdgeLabelControl, IAttachableControl<EdgeControl>
    {
        /// <summary>
        /// Gets label attach node
        /// </summary>
        public EdgeControl? AttachNode
        {
            get => (EdgeControl)GetValue(AttachNodeProperty);
            private set => SetValue(AttachNodeProperty, value);
        }

        public static readonly StyledProperty<> AttachNodeProperty = AvaloniaProperty.Register(nameof(AttachNode),
            typeof(EdgeControl), typeof(AttachableEdgeLabelControl),
            new PropertyMetadata(null));

        static AttachableEdgeLabelControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AttachableEdgeLabelControl),
                new FrameworkPropertyMetadata(typeof(AttachableEdgeLabelControl)));
        }

        public AttachableEdgeLabelControl()
        {
            DataContext = this;
        }

        /// <summary>
        /// Attach label to VertexControl
        /// </summary>
        /// <param name="node">VertexControl node</param>
        public virtual void Attach(EdgeControl node)
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
            if (AttachNode!.IsVisible && ShowLabel)
                Show();
            else if (!AttachNode.IsVisible)
            {
                Hide();
            }
        }

        protected override EdgeControl GetEdgeControl(Control? parent)
        {
            if (AttachNode == null)
                throw new GX_InvalidDataException("AttachableEdgeLabelControl node is not attached!");
            return AttachNode;
        }
    }
}