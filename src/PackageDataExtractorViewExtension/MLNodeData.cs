using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageDataExtractor
{
    public class MlNode
    {
        public string functionString { get; set; }
        public MlNodeData nodeData { get; set; }
    }

    public class MlNodeData
    {
        public string nodeType { get; set; }
        public string packageName { get; set; }
        public string packageVersion { get; set; }
    }
}
