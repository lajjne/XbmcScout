using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core.Providers {
    public interface IMovieMetadataProvider : IMetadataProvider {
        MovieXML[] Search(String MovieName);
        MovieXML Get(String MovieID);
    }
}
