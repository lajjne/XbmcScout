using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core.Providers {

    public interface IMetadataProvider {
        string Name { get; }
        string Version { get; }
        string Url { get; }
    }
}
