using System;
using System.ComponentModel;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
	public abstract class LayoutParametersBase : ILayoutParameters
	{
	    protected LayoutParametersBase()
        {
            Seed = Guid.NewGuid().GetHashCode();
        }

        #region ICloneable Members

        public object Clone()
		{
			return MemberwiseClone();
		}

        public int Seed { get; set; }
		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string propertyName)
		{
			//delegating to the event...
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}