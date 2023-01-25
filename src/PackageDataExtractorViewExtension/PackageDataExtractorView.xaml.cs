using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Dynamo.UI;
using Dynamo.ViewModels;

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
