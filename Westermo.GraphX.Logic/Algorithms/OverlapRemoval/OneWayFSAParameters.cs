using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval;

public enum OneWayFSAWayEnum {
	Horizontal,
	Vertical
}

public class OneWayFSAParameters : OverlapRemovalParameters
{
	private OneWayFSAWayEnum _way;
	public OneWayFSAWayEnum Way
	{
		get => _way;
		set
		{
			if ( _way != value )
			{
				_way = value;
				NotifyChanged( "Way" );
			}
		}
	}

	/// <summary>
	/// Returns <paramref name="parameters"/> as <see cref="OneWayFSAParameters"/>. Parameters of another type are
	/// converted to a new instance that keeps their gap settings rather than silently discarding them.
	/// </summary>
	public static OneWayFSAParameters From( IOverlapRemovalParameters parameters )
	{
		return parameters switch
		{
			OneWayFSAParameters oneWay => oneWay,
			null => new OneWayFSAParameters(),
			_ => new OneWayFSAParameters { HorizontalGap = parameters.HorizontalGap, VerticalGap = parameters.VerticalGap },
		};
	}
}