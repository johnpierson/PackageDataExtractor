using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Core;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Nodes.CustomNodes;
using Dynamo.Graph.Nodes.ZeroTouch;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.PackageManager;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal PackageManagerExtension PackageManager;
        /// <summary>
        /// Loaded packages for export
        /// </summary>
        public ObservableCollection<string> LoadedPackages { get; set; }

        private string _selectedPackage;
        public string SelectedPackage
        {
            get { return _selectedPackage; }
            set
            {
                // Some logic here
                _selectedPackage = value;
                PackageNodes = GetPackageNodes();
            }
        }

        /// <summary>
        /// Selected nodes for export
        /// </summary>
        public ObservableCollection<MlNode> PackageNodes { get; set; }

        public PackageDataExtractorViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            viewLoadedParamsInstance = p;

            DynamoViewModel = viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;
            PackageManager = viewLoadedParamsInstance.ViewStartupParams.ExtensionManager.Extensions.OfType<PackageManagerExtension>().FirstOrDefault();

            LoadedPackages = GetLoadedPackages();
        }

        private ObservableCollection<string> GetLoadedPackages()
        {
            var packagess = PackageManager.PackageLoader.LocalPackages.ToList();

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

        private ObservableCollection<MlNode> GetPackageNodes()
        {
            List<MlNode> nodeData = new List<MlNode>();

            var libraries = DynamoViewModel.Model.SearchModel.SearchEntries.ToList();

            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) ||
                    element.ElementType.HasFlag(ElementTypes.CustomNode) && element.IsVisibleInSearch) 
                {
                    if (element.Categories.First().Equals(SelectedPackage))
                    {
                        var dynMethod = element.GetType().GetMethod("ConstructNewNodeModel",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        var obj = dynMethod.Invoke(element, new object[] { });
                        var nM = obj as NodeModel;

                        MlNode mlNode = new MlNode();

                        if (nM is DSFunction dsFunction)
                        {
                            mlNode.functionString = dsFunction.FunctionSignature;
                        }

                        if (nM is Function function)
                        {
                            mlNode.functionString = function.FunctionSignature.ToString();
                        }


                        nodeData.Add(new MlNode());
                    }
                  
                }
            }

            return nodeData.ToObservableCollection();
        }
        public void Dispose()
        {
            DynamoViewModel = null;
        }
    }
}
