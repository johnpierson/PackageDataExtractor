using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using Dynamo.UI.Commands;
using Newtonsoft.Json;
using System.Web;
using Dynamo.Controls;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal PackageManagerExtension PackageManager;

        public DelegateCommand ExportJsonCommand { get; set; }


        private bool _canExport;

        /// <summary>
        /// Checks if the export operation can be completed
        /// </summary>
        public bool CanExport
        {
            get
            {
                if (CurrentJson != null)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(JsonFilePath))
                {
                    return true;
                }

                return false;
            }
            private set
            {
                if (_canExport != value)
                {
                    _canExport = value;
                    RaisePropertyChanged(nameof(CanExport));
                }
            }
        }

        /// <summary>
        /// Loaded packages for export
        /// </summary>
        public ObservableCollection<string> LoadedPackages { get; set; }

        private string _selectedPackage;
        public string SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                // Some logic here
                _selectedPackage = value;
                PackageNodes = GetPackageNodes();
                RaisePropertyChanged(nameof(PackageNodes));
                JsonFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\{SelectedPackage}.json";
                RaisePropertyChanged(nameof(JsonFilePath));
                RaisePropertyChanged(nameof(CanExport));
            }
        }
        /// <summary>
        /// Current Json string for export
        /// </summary>
        public string CurrentJson { get; set; } = String.Empty;

        /// <summary>
        /// File path for export. (builds dynamically from package name selected)
        /// </summary>
        public string JsonFilePath { get; set; }

        /// <summary>
        /// Selected nodes for export
        /// </summary>
        public ObservableCollection<MlNode> PackageNodes { get; set; } = new ObservableCollection<MlNode>();

        /// <summary>
        /// Selected nodes for export
        /// </summary>
        public List<NodeSearchElement> CustomNodesToSearch { get; set; }


        public PackageDataExtractorViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            viewLoadedParamsInstance = p;

            DynamoViewModel = viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;
            PackageManager = viewLoadedParamsInstance.ViewStartupParams.ExtensionManager.Extensions.OfType<PackageManagerExtension>().FirstOrDefault();

            LoadedPackages = GetLoadedPackages();
            ExportJsonCommand = new DelegateCommand(ExportJson);
        }

        private ObservableCollection<string> GetLoadedPackages()
        {
            List<string> packages = new List<string>();
            List<NodeSearchElement> nodesToSearch = new List<NodeSearchElement>();
            var libraries = DynamoViewModel.Model.SearchModel.SearchEntries.ToList();

            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) ||
                    element.ElementType.HasFlag(ElementTypes.CustomNode))
                {
                    packages.Add(element.Categories.First());
                    nodesToSearch.Add(element);
                }
            }

            CustomNodesToSearch = nodesToSearch;

            return packages.Distinct().ToObservableCollection();
        }

        private ObservableCollection<MlNode> GetPackageNodes()
        {
            var packages = PackageManager.PackageLoader.LocalPackages.ToList();

            //data to write
            List<MlNode> nodeData = new List<MlNode>();
            Dictionary<string, MlNodeData> jsonDataDictionary = new Dictionary<string, MlNodeData>();

            foreach (var element in CustomNodesToSearch)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) ||
                    element.ElementType.HasFlag(ElementTypes.CustomNode) && element.IsVisibleInSearch)
                {
                    if (!element.Categories.First().Equals(SelectedPackage)) continue;


                    //build a node model, there is probably an easier way to do this, but ah well
                    var dynMethod = element.GetType().GetMethod("ConstructNewNodeModel",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var obj = dynMethod.Invoke(element, new object[] { });
                    var nM = obj as NodeModel;

                    //custom class to serialize this
                    MlNode mlNode = new MlNode();
                    MlNodeData mlNodeData = new MlNodeData();

                    switch (nM)
                    {
                        case DSFunction dsFunction:
                            mlNode.Name = dsFunction.FunctionSignature;
                            mlNodeData.NodeType = "FunctionNode";
                            break;
                        case Function function:
                            mlNode.Name = function.FunctionSignature.ToString();
                            mlNodeData.NodeType = "FunctionNode";
                            break;
                        default:
                            var typeOfNode = nM.GetType();
                            var property = typeOfNode.GetProperty("FullName");

                            if (property != null)
                            {
                                var value = property.GetValue(nM, null);
                                mlNode.Name = value.ToString();
                                mlNodeData.NodeType = typeOfNode.ToString();
                            }

                            break;
                        //TODO: Find concrete type for other nodes
                    }

                    if(string.IsNullOrWhiteSpace(mlNode.Name)) continue;

                    //store the creation name for preview
                    if (nM != null) mlNode.CreationName = nM.Name;

                    //get the version
                    var package = packages.First(p => p.Name.Contains(SelectedPackage));
                    mlNodeData.PackageName = package.Name;
                    mlNodeData.PackageVersion = package.VersionName;

                    jsonDataDictionary.Add(mlNode.Name, mlNodeData);

                    mlNode.nodeData = mlNodeData;

                    nodeData.Add(mlNode);

                }
            }

            CurrentJson = JsonConvert.SerializeObject(jsonDataDictionary);
            RaisePropertyChanged(nameof(CurrentJson));


            return nodeData.ToObservableCollection();
        }

        /// <summary>
        ///     The main method executing the export
        /// </summary>
        /// <param name="obj"></param>
        private void ExportJson(object obj)
        {
            File.WriteAllText(JsonFilePath, CurrentJson);
        }

        public void Dispose()
        {
            DynamoViewModel = null;
        }
    }
}
