namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class BalloonTreeLayoutParameters : LayoutParametersBase
{
	internal int minRadius = 2;
	internal float border = 20.0f;

	public int MinRadius
	{
		get => minRadius;
		set
		{
			if (value == minRadius) return;
			minRadius = value;
			NotifyPropertyChanged( "MinRadius" );
		}
	}


	public float Border
	{
		get => border;
		set
		{
			if (value == border) return;
			border = value;
			NotifyPropertyChanged( "Border" );
		}
	}
}