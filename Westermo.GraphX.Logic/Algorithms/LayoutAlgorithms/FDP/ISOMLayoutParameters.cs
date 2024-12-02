namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
	public class ISOMLayoutParameters : LayoutParametersBase
	{
		private double _width = 300;
		/// <summary>
		/// Width of the bounding box. Default value is 300.
		/// </summary>
		public double Width
		{
			get => _width;
			set
			{
				_width = value;
				NotifyPropertyChanged("Width");
			}
		}

		private double _height = 300;
		/// <summary>
		/// Height of the bounding box. Default value is 300.
		/// </summary>
		public double Height
		{
			get => _height;
			set
			{
				_height = value;
				NotifyPropertyChanged("Height");
			}
		}

		private int _maxEpoch = 2000;
		/// <summary>
		/// Maximum iteration number. Default value is 2000.
		/// </summary>
		public int MaxEpoch
		{
			get => _maxEpoch;
			set
			{
				_maxEpoch = value;
				NotifyPropertyChanged("MaxEpoch");
			}
		}

		private int _radiusConstantTime = 100;
		/// <summary>
		/// Radius constant time. Default value is 100.
		/// </summary>
		public int RadiusConstantTime
		{
			get => _radiusConstantTime;
			set
			{
				_radiusConstantTime = value;
				NotifyPropertyChanged("RadiusConstantTime");
			}
		}

		private int _initialRadius = 5;
		/// <summary>
		/// Default value is 5.
		/// </summary>
		public int InitialRadius
		{
			get => _initialRadius;
			set
			{
				_initialRadius = value;
				NotifyPropertyChanged("InitialRadius");
			}
		}

		private int _minRadius = 1;
		/// <summary>
		/// Minimal radius. Default value is 1.
		/// </summary>
		public int MinRadius
		{
			get => _minRadius;
			set
			{
				_minRadius = value;
				NotifyPropertyChanged("MinRadius");
			}
		}

		private double _initialAdaption = 0.9;
		/// <summary>
		/// Default value is 0.9.
		/// </summary>
		public double InitialAdaption
		{
			get => _initialAdaption;
			set
			{
				_initialAdaption = value;
				NotifyPropertyChanged("InitialAdaption");
			}
		}

		private double _minAdaption;
		/// <summary>
		/// Default value is 0.
		/// </summary>
		public double MinAdaption
		{
			get => _minAdaption;
			set
			{
				_minAdaption = value;
				NotifyPropertyChanged("MinAdaption");
			}
		}

		private double _coolingFactor = 2;
		/// <summary>
		/// Default value is 2.
		/// </summary>
		public double CoolingFactor
		{
			get => _coolingFactor;
			set
			{
				_coolingFactor = value;
				NotifyPropertyChanged("CoolingFactor");
			}
		}
	}
}