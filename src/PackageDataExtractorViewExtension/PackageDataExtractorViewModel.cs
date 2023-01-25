using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Package = Dynamo.PackageManager.Package;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private  ViewLoadedParams _viewLoadedParamsInstance;
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
                if (value is null)
                {
                    _selectedPackage = value;
                    PackageNodes = GetPackageNodes();
                    RaisePropertyChanged(nameof(PackageNodes));

                    return;
                }
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

        public PackageDataExtractorViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            _viewLoadedParamsInstance = p;

            DynamoViewModel = _viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;
            PackageManager = DynamoViewModel.Model.GetPackageManagerExtension();

            //subscribe to package manager events
            PackageManager.PackageLoader.PackgeLoaded += OnPackageChange;
            PackageManager.PackageLoader.PackageRemoved += OnPackageChange;
          
            LoadedPackages = PackageManager.PackageLoader.LocalPackages.Where(HasNodes).ToObservableCollection();
            ExportJsonCommand = new DelegateCommand(ExportJson);
        }
        /// <summary>
        /// Reload the package list on a change of the packages loaded.
        /// </summary>
        /// <param name="obj"></param>
        private void OnPackageChange(Package obj)
        {
            if (SelectedPackage != null)
            {
                if (SelectedPackage == obj)
                {
                    SelectedPackage = null;
                    JsonFilePath = string.Empty;
                    RaisePropertyChanged(nameof(JsonFilePath));
                    RaisePropertyChanged(nameof(CanExport));
                }
            }
            
            LoadedPackages = PackageManager.PackageLoader.LocalPackages.Where(HasNodes).ToObservableCollection();
            RaisePropertyChanged(nameof(LoadedPackages));
            RaisePropertyChanged(nameof(SelectedPackage));
        }
        /// <summary>
        /// Collect the relevant nodes for the selected package.
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<MlNode> GetPackageNodes()
        {
            if(SelectedPackage is null) return new ObservableCollection<MlNode>();

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
                    if (skip) continue;

                    //build a node model, there is probably an easier way to do this, but ah well TODO: Check with the team about making this a bit nicer.
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
                    }

                    if (string.IsNullOrWhiteSpace(mlNode.Name)) continue;

                    //store the creation name for preview
                    if (nodeModel != null) mlNode.CreationName = nodeModel.Name;

                    //get the package name and version
                    var package = SelectedPackage;
                    mlNodeData.PackageName = package.Name;
                    mlNodeData.PackageVersion = package.VersionName;

                    //if for some reason, multiple nodes exist with the same signature, skip it
                    if (!jsonDataDictionary.ContainsKey(mlNode.Name))
                    {
                        jsonDataDictionary.Add(mlNode.Name, mlNodeData);
                    }

                    //the node data gets displayed in the window, and that's pretty much it
                    mlNode.nodeData = mlNodeData;
                    nodeData.Add(mlNode);
                }
            }

            CurrentJson = JsonConvert.SerializeObject(jsonDataDictionary);
            RaisePropertyChanged(nameof(CurrentJson));


            return nodeData.ToObservableCollection();
        }

        /// <summary>
        /// The main method executing the export
        /// </summary>
        /// <param name="obj"></param>
        private void ExportJson(object obj)
        {
            File.WriteAllText(JsonFilePath, CurrentJson);
        }

        public void Dispose()
        {
            DynamoViewModel = null;
            //unsubscribe to package manager events
            PackageManager.PackageLoader.PackgeLoaded -= OnPackageChange;
            PackageManager.PackageLoader.PackageRemoved -= OnPackageChange;
            PackageManager = null;
        }

        /// <summary>
        /// Check if the package has nodes in it. This way we don't show extensions in the dropdown too.
        /// </summary>
        /// <param name="package">The package to check.</param>
        /// <returns name="result">If it is a package with nodes, we will return a result.</returns>
        private bool HasNodes(Package package)
        {
            //this section flags the easier (dyf-based) packages. Zero touch requires a bit more searching.
            if (package.LoadedCustomNodes.Any())
            {
                return true;
            }
            
            var customNodes = DynamoViewModel.Model.SearchModel.SearchEntries.Where(s =>
                s.IsVisibleInSearch && (s.ElementType.HasFlag(ElementTypes.Packaged) ||
                                        s.ElementType.HasFlag(ElementTypes.CustomNode)));

            foreach (var nodeSearchElement in customNodes)
            {
                switch (nodeSearchElement)
                {
                    case CustomNodeSearchElement customNodeSearchElement:
                        if (customNodeSearchElement.Path.Contains(package.RootDirectory))
                        {
                            return true;
                        }
                        break;
                    case ZeroTouchSearchElement zeroTouchSearchElement:
                        if (zeroTouchSearchElement.Assembly.Contains(package.RootDirectory))
                        {
                            return true;
                        }
                        break;
                    case NodeModelSearchElement nodeModelSearchElement:
                        if (nodeModelSearchElement.Assembly.Contains(package.RootDirectory))
                        {
                            return true;
                        }
                        break;
                    default:
                        return false;
                }
            }

            return false;
        }
    }
}
