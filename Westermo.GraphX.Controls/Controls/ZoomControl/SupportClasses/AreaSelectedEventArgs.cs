using System.Windows;

namespace Westermo.GraphX.Controls
{
    public class AreaSelectedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Rectangle data in coordinates of content object
        /// </summary>
        public Rect Rectangle { get; set; }

        public AreaSelectedEventArgs(Rect rec)
        {
            Rectangle = rec;
        }
    }
}
