using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Core;
using Dynamo.Wpf.Extensions;

namespace PackageDataExtractor
{
    public class PackageDataExtractorViewModel : NotificationObject, IDisposable
    {
        private readonly ViewLoadedParams viewLoadedParamsInstance;

        public PackageDataExtractorViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            viewLoadedParamsInstance = p;

        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
