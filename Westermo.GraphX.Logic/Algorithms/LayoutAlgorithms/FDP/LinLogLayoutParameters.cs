namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
	public class LinLogLayoutParameters : LayoutParametersBase
	{
		internal double attractionExponent = 1.0;

		public double AttractionExponent
		{
			get => attractionExponent;
			set
			{
				attractionExponent = value;
				NotifyPropertyChanged("AttractionExponent");
			}
		}

		internal double repulsiveExponent;

		public double RepulsiveExponent
		{
			get => repulsiveExponent;
			set
			{
				repulsiveExponent = value;
				NotifyPropertyChanged("RepulsiveExponent");
			}
		}

		internal double gravitationMultiplier = 0.1;

		public double GravitationMultiplier
		{
			get => gravitationMultiplier;
			set
			{
				gravitationMultiplier = value;
				NotifyPropertyChanged("GravitationMultiplier");
			}
		}

		internal int iterationCount = 100;

		public int IterationCount
		{
			get => iterationCount;
			set
			{
				iterationCount = value;
				NotifyPropertyChanged("IterationCount");
			}
		}
	}
}