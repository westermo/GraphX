using Westermo.GraphX.Common.Models;

namespace ShowcaseApp.WPF
{
    public class DataVertex(string text = "") : VertexBase
    {
        public string Text { get; set; } = string.IsNullOrEmpty(text) ? "New Vertex" : text;
        public string Name { get; set; }
        public string Profession { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public int ImageId { get; set; }

        public bool IsBlue { get; set; }

        #region Calculated or static props

        public override string ToString()
        {
            return Text;
        }

        #endregion

        /// <summary>
        /// Default constructor for this class
        /// (required for serialization).
        /// </summary>
        public DataVertex():this(string.Empty)
        {
        }
    }
}
