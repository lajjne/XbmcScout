using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core.Providers {
    public interface IMetadataProvider {
        String name {
            get;
        }
        String version {
            get;
        }
        String url {
            get;
        }
    }
}
