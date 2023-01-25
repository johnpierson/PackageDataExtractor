using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageDataExtractor
{
    public class MlNode
    {
        public string Name { get; set; }
        public string CreationName { get; set; }
        public MlNodeData nodeData { get; set; }
    }

    public class MlNodeData
    {
        [JsonProperty("nodeType")]
        public string NodeType { get; set; }
        [JsonProperty("packageName")]
        public string PackageName { get; set; }
        [JsonProperty("packageVersion")]
        public string PackageVersion { get; set; }
    }
}
