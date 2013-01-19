using System;
using System.Collections.Generic;
using System.Text;

namespace MediaScout.Providers
{
    public interface IMetadataProvider
    {
        String name
        {
            get;
        }
        String version
        {
            get;
        }
        String url
        {
            get;
        }
    }
}
