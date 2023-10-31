using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
	/// <summary>
	/// A System.Windows.Rect egy strukt�ra, ez�rt a heap-en t�rol�dik. Bizonyos esetekben ez nem
	/// szerencs�s, �gy sz�ks�g van erre a wrapper oszt�lyra. Mivel ez class, ez�rt nem
	/// �rt�k szerinti �tad�s van.
	/// </summary>
	public class RectangleWrapper<TObject>
		where TObject : class
	{
		private readonly TObject id;
		public TObject Id
		{
			get { return id; }
		}

		public Rect Rectangle;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rectangle"></param>
		/// <param name="id">Az adott t�glalap azonos�t�ja (az overlap-removal v�g�n tudnunk kell, hogy 
		/// melyik t�glalap melyik objektumhoz tartozik. Az azonos�t�s megoldhat� lesz id alapj�n.</param>
		public RectangleWrapper( Rect rectangle, TObject id )
		{
			Rectangle = rectangle;
			this.id = id;
		}

		public double CenterX
		{
			get { return Rectangle.Left + Rectangle.Width / 2; }
		}

		public double CenterY
		{
			get { return Rectangle.Top + Rectangle.Height / 2; }
		}

		public Point Center
		{
			get { return new Point( CenterX, CenterY ); }
		}
	}
}