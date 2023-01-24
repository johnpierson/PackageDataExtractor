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

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal PackageManagerExtension PackageManager;

        public DelegateCommand ExportJsonCommand { get; set; }


        private bool canExport;

        /// <summary>
        ///     Checks if both folder paths have been set
        /// </summary>
        public bool CanExport
        {
            get
            {
                if (CurrentJSON == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(JsonFilePath))
                {
                    return false;
                }

                return true;
            }
            private set
            {
                if (canExport != value)
                {
                    canExport = value;
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

        public string CurrentJSON { get; set; } = String.Empty;
        public string JsonFilePath { get; set; }

        /// <summary>
        /// Selected nodes for export
        /// </summary>
        public ObservableCollection<MlNode> PackageNodes { get; set; } = new ObservableCollection<MlNode>();

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
            var packages = PackageManager.PackageLoader.LocalPackages.ToList();

            //data to write
            List<MlNode> nodeData = new List<MlNode>();
            Dictionary<string, MlNodeData> jsonDataDictionary = new Dictionary<string, MlNodeData>();

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
                        MlNodeData mlNodeData = new MlNodeData();
                        if (nM is DSFunction dsFunction)
                        {
                            mlNode.Name = dsFunction.FunctionSignature;
                            mlNodeData.NodeType = "FunctionNode";
                        }

                        if (nM is Function function)
                        {
                            mlNode.Name = function.FunctionSignature.ToString();
                            mlNodeData.NodeType = "FunctionNode";
                        }

                        //get the version
                        var package = packages.First(p => p.Name.Contains(SelectedPackage));
                        mlNodeData.PackageName = package.Name;
                        mlNodeData.PackageVersion = package.VersionName;

                        jsonDataDictionary.Add(mlNode.Name, mlNodeData);

                        mlNode.nodeData = mlNodeData;
                        
                        nodeData.Add(mlNode);
                    }
                  
                }
            }

            CurrentJSON = JsonConvert.SerializeObject(jsonDataDictionary);
            RaisePropertyChanged(nameof(CurrentJSON));

            return nodeData.ToObservableCollection();
        }

        /// <summary>
        ///     The main method executing the export
        /// </summary>
        /// <param name="obj"></param>
        private void ExportJson(object obj)
        {
            File.WriteAllText(JsonFilePath,CurrentJSON);
        }

        public void Dispose()
        {
            DynamoViewModel = null;
        }
    }
}
