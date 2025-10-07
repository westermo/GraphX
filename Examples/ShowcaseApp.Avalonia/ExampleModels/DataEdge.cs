using System;
using System.ComponentModel;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Measure;

namespace ShowcaseApp.Avalonia.ExampleModels
{
    [Serializable]
    public class DataEdge : EdgeBase<DataVertex>, INotifyPropertyChanged
    {
        public override Point[] RoutingPoints { get; set; } = [];

        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
            Angle = 90;
        }

        public DataEdge()
            : base(null!, null!)
        {
            Angle = 90;
        }

        public bool ArrowTarget { get; set; }

        public double Angle { get; set; }

        /// <summary>
        /// Node main description (header)
        /// </summary>
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public string ToolTipText { get; set; } = string.Empty;

        public override string ToString()
        {
            return Text;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}