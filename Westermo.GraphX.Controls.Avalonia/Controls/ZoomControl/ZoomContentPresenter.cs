using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class ZoomContentPresenter : ContentPresenter, INotifyPropertyChanged
    {
        public event ContentSizeChangedHandler? ContentSizeChanged;

        private Size _contentSize;

        public Size ContentSize
        {
            get => _contentSize;
            private set
            {
                if (value == _contentSize)
                    return;

                _contentSize = value;
                ContentSizeChanged?.Invoke(this, _contentSize);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            base.MeasureOverride(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var max = 1000000000;
            var x = double.IsInfinity(constraint.Width) ? max : constraint.Width;
            var y = double.IsInfinity(constraint.Height) ? max : constraint.Height;
            return new Size(x, y);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (this.GetVisualChildren().FirstOrDefault() is not Control child)
                return arrangeBounds;

            //set the ContentSize
            ContentSize = child.DesiredSize;
            child.Arrange(new Rect(child.DesiredSize));

            return arrangeBounds;
        }
    }
}
