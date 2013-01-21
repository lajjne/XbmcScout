using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core.Providers {
    public interface IMovieMetadataProvider : IMetadataProvider {

        MovieXML[] Search(string MovieName);
        MovieXML Get(string MovieID);
    }
}
