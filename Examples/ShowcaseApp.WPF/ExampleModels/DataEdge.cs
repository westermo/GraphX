﻿using System.ComponentModel;
using System;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Models;
using ShowcaseApp.WPF.FileSerialization;
using YAXLib.Attributes;

namespace ShowcaseApp.WPF
{
    [Serializable]
    public class DataEdge : EdgeBase<DataVertex>, INotifyPropertyChanged
    {
        [YAXCustomSerializer(typeof(YAXPointArraySerializer))]
        public override Point[] RoutingPoints { get; set; }

        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
            Angle = 90;
        }

        public DataEdge()
            : base(null, null)
        {
            Angle = 90;
        }

        public bool ArrowTarget { get; set; }

        public double Angle { get; set; }

        /// <summary>
        /// Node main description (header)
        /// </summary>
        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public string ToolTipText { get; set; }

        public override string ToString()
        {
            return Text;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}