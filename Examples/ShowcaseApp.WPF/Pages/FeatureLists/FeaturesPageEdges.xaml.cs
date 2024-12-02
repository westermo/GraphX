﻿using System.ComponentModel;
using System.Windows.Controls;

namespace ShowcaseApp.WPF.Pages.FeatureLists
{
    /// <summary>
    /// Interaction logic for DebugGraph.xaml
    /// </summary>
    public partial class FeaturesPageEdges : INotifyPropertyChanged
    {
        public FeaturesPageEdges()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}