using System.Collections.Generic;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
	public abstract class OverlapRemovalAlgorithmBase<TObject, TParam>(
		IDictionary<TObject, Rect> rectangles,
		TParam parameters) : AlgorithmBase, IOverlapRemovalAlgorithm<TObject, TParam>
		where TObject : class
		where TParam : IOverlapRemovalParameters
	{
		protected IDictionary<TObject, Rect> OriginalRectangles = rectangles;
		public IDictionary<TObject, Rect> Rectangles
		{
			get => OriginalRectangles;
			set => OriginalRectangles = value;
		}

        /// <summary>
        /// Algorithm parameters
        /// </summary>
		public TParam Parameters { get; private set; } = parameters;

		public IOverlapRemovalParameters GetParameters()
		{
			return Parameters;
		}

		protected List<RectangleWrapper<TObject>> WrappedRectangles;

		/// <summary>
        /// Initialize algorithm initial data
        /// </summary>
        /// <param name="rectangles">Size rectangles</param>
        /// <param name="parameters">algorithm parameters</param>
	    public void Initialize(IDictionary<TObject, Rect> rectangles, TParam parameters)
	    {
            OriginalRectangles = rectangles;
            Parameters = parameters;	        
	    }

        /// <summary>
        /// Initialize algorithm initial data
        /// </summary>
        /// <param name="rectangles">Size rectangles</param>
        public void Initialize(IDictionary<TObject, Rect> rectangles)
        {
            OriginalRectangles = rectangles;
        }

        private void GenerateWrappedRectangles(IDictionary<TObject, Rect> rectangles)
        {
            //wrapping the old rectangles, to remember which one belongs to which object
            WrappedRectangles = [];
            var i = 0;
            foreach (var kvpRect in rectangles)
            {
                WrappedRectangles.Insert(i, new RectangleWrapper<TObject>(kvpRect.Value, kvpRect.Key));
                i++;
            }
        }

        public sealed override void Compute(CancellationToken cancellationToken)
		{
            GenerateWrappedRectangles(OriginalRectangles);

			AddGaps();

			RemoveOverlap(cancellationToken);

			RemoveGaps();

			foreach ( var r in WrappedRectangles )
				OriginalRectangles[r.Id] = r.Rectangle;
		}

		protected virtual void AddGaps()
		{
			foreach ( var r in WrappedRectangles )
			{
				r.Rectangle.Width += Parameters.HorizontalGap;
				r.Rectangle.Height += Parameters.VerticalGap;
				r.Rectangle.Offset( -Parameters.HorizontalGap / 2, -Parameters.VerticalGap / 2 );
			}
		}

		protected virtual void RemoveGaps()
		{
			foreach ( var r in WrappedRectangles )
			{
				r.Rectangle.Width -= Parameters.HorizontalGap;
				r.Rectangle.Height -= Parameters.VerticalGap;
				r.Rectangle.Offset( Parameters.HorizontalGap / 2, Parameters.VerticalGap / 2 );
			}
		}

		protected abstract void RemoveOverlap(CancellationToken cancellationToken);
	}
}