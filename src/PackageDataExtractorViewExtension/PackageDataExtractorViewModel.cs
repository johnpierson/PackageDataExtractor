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
using Package = Dynamo.PackageManager.Package;

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
        public ObservableCollection<Package> LoadedPackages { get; set; }

        private Package _selectedPackage;
        public Package SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                // Some logic here
                _selectedPackage = value;
                PackageNodes = GetPackageNodes();
                RaisePropertyChanged(nameof(PackageNodes));
                JsonFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\{SelectedPackage.Name}.json";
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

            LoadedPackages = PackageManager.PackageLoader.LocalPackages.ToObservableCollection();
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
            //var packages = PackageManager.PackageLoader.LocalPackages.ToList();

            //data to write
            List<MlNode> nodeData = new List<MlNode>();
            Dictionary<string, MlNodeData> jsonDataDictionary = new Dictionary<string, MlNodeData>();

            foreach (var element in DynamoViewModel.Model.SearchModel.SearchEntries.ToList())
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) ||
                    element.ElementType.HasFlag(ElementTypes.CustomNode) && element.IsVisibleInSearch)
                {
                    bool skip = true;

                    switch (element)
                    {
                        case CustomNodeSearchElement customNodeSearchElement:
                            skip = !customNodeSearchElement.Path.Contains(SelectedPackage.RootDirectory);
                            break;
                        case ZeroTouchSearchElement zeroTouchSearchElement:
                            skip = !zeroTouchSearchElement.Assembly.Contains(SelectedPackage.RootDirectory);
                            break;
                        case NodeModelSearchElement nodeModelSearchElement:
                            skip = !nodeModelSearchElement.Assembly.Contains(SelectedPackage.RootDirectory);
                            break;
                    }
                    if(skip) continue;
                    
                    //build a node model, there is probably an easier way to do this, but ah well
                    NodeModel nodeModel;
                    try
                    {
                        var dynMethod = element.GetType().GetMethod("ConstructNewNodeModel",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        var obj = dynMethod.Invoke(element, new object[] { });
                        nodeModel = obj as NodeModel;
                    }
                    catch (Exception)
                    {
                        nodeModel = null;
                    }

                    if (nodeModel == null) continue;

                    //custom class to serialize this
                    MlNode mlNode = new MlNode();
                    MlNodeData mlNodeData = new MlNodeData();

                    switch (nodeModel)
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
                            mlNode.Name = $"{nodeModel.GetType().FullName}, {nodeModel.GetType().Assembly.GetName().Name}";
                            mlNodeData.NodeType = "ExtensionNode";

                            break;
                        //TODO: Find concrete type for other nodes
                    }

                    if(string.IsNullOrWhiteSpace(mlNode.Name)) continue;

                    //store the creation name for preview
                    if (nodeModel != null) mlNode.CreationName = nodeModel.Name;

                    //get the version
                    var package = SelectedPackage;
                    mlNodeData.PackageName = package.Name;
                    mlNodeData.PackageVersion = package.VersionName;

                    if (!jsonDataDictionary.ContainsKey(mlNode.Name))
                    {
                        jsonDataDictionary.Add(mlNode.Name, mlNodeData);
                    }
                   

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
