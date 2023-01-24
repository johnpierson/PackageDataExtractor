using Dynamo.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewExtension : ViewExtensionBase, IViewExtension
    {
        public MenuItem PackageDataExtractorMenuItem;
        private ViewLoadedParams viewLoadedParamsReference;

        internal PackageDataExtractorView View;
        internal PackageDataExtractorViewModel ViewModel;

        public PackageDataExtractorViewExtension()
        {
            InitializeViewExtension();
        }
        /// <summary>
        /// Extension Name
        /// </summary>
        public override string Name => Properties.Resources.ExtensionName;

        /// <summary>
        /// GUID of the extension
        /// </summary>
        public override string UniqueId => "6647180B-70D8-4463-B208-6DA6140D9CB5";


        public override void Dispose()
        {
            // Cleanup
            if (PackageDataExtractorMenuItem != null)
            {
                PackageDataExtractorMenuItem.Checked -= MenuItemCheckHandler;
                PackageDataExtractorMenuItem.Unchecked -= MenuItemUnCheckHandler;
            }

            ViewModel?.Dispose();
            View = null;
            ViewModel = null;
        }
        public override void Loaded(ViewLoadedParams viewLoadedParams)
        {
            if (viewLoadedParams == null) throw new ArgumentNullException(nameof(viewLoadedParams));

            viewLoadedParamsReference = viewLoadedParams;

            // Add a button to Dynamo View menu to manually show the window
            PackageDataExtractorMenuItem = new MenuItem { Header = Properties.Resources.HeaderText, IsCheckable = true };
            PackageDataExtractorMenuItem.Checked += MenuItemCheckHandler;
            PackageDataExtractorMenuItem.Unchecked += MenuItemUnCheckHandler;

            viewLoadedParamsReference.AddExtensionMenuItem(PackageDataExtractorMenuItem);
        }

        public override void Shutdown()
        {
            Dispose();
        }

        private void InitializeViewExtension()
        {
            ViewModel = new PackageDataExtractorViewModel(viewLoadedParamsReference);
            View = new PackageDataExtractorView(ViewModel);
        }

        private void MenuItemCheckHandler(object sender, RoutedEventArgs e)
        {
            AddToSidebar();
        }

        private void MenuItemUnCheckHandler(object sender, RoutedEventArgs e)
        {
            this.Dispose();

            viewLoadedParamsReference.CloseExtensioninInSideBar(this);
        }
        private void AddToSidebar()
        {
            InitializeViewExtension();

            viewLoadedParamsReference?.AddToExtensionsSideBar(this, View);
        }

        public override void Closed()
        {
            if (PackageDataExtractorMenuItem != null) PackageDataExtractorMenuItem.IsChecked = false;
        }
    }
}
