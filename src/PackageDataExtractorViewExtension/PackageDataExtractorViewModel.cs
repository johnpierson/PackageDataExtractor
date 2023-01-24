using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Core;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;

        /// <summary>
        /// Loaded packages for export
        /// </summary>
        public ObservableCollection<string> LoadedPackages { get; set; }

        /// <summary>
        /// Selected nodes for export
        /// </summary>
        public ObservableCollection<NodeSearchElement> PackageNodes { get; set; }

        public PackageDataExtractorViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            viewLoadedParamsInstance = p;

            DynamoViewModel = viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;

            LoadedPackages = GetLoadedPackages();
        }

        private ObservableCollection<string> GetLoadedPackages()
        {
            List<string> packages = new List<string>();

            var libraries = DynamoViewModel.Model.SearchModel.SearchEntries.ToList();

            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) ||
                    element.ElementType.HasFlag(ElementTypes.CustomNode))
                {
                    packages.Add(element.Categories.First());
                }
            }

            return packages.Distinct().ToObservableCollection();
        }
        public void Dispose()
        {
            DynamoViewModel = null;
        }
    }
}
