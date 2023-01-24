﻿using System.Windows.Controls;

namespace PackageDataExtractor
{
    /// <summary>
    /// Interaction logic for PackageDataExtractorView.xaml
    /// </summary>
    public partial class PackageDataExtractorView : UserControl
    {
        public PackageDataExtractorView(PackageDataExtractorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}