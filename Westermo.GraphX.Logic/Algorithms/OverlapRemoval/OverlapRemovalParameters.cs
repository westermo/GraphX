using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
	public class OverlapRemovalParameters : IOverlapRemovalParameters
	{
		private float _verticalGap = 10;
		private float _horizontalGap = 10;

        /// <summary>
        /// Gets or sets minimal vertical distance between vertices
        /// </summary>
		public float VerticalGap
		{
			get => _verticalGap;
			set
			{
			    if (_verticalGap == value) return;
			    _verticalGap = value;
			    NotifyChanged( "VerticalGap" );
			}
		}

        /// <summary>
        /// Gets or sets minimal horizontal distance between vertices
        /// </summary>
		public float HorizontalGap
		{
			get => _horizontalGap;
			set
			{
			    if (_horizontalGap == value) return;
			    _horizontalGap = value;
			    NotifyChanged( "HorizontalGap" );
			}
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		protected void NotifyChanged( string propertyName )
		{
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs( propertyName ));
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	}
}