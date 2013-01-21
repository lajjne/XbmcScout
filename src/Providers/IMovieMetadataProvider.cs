using System;
using System.Collections.Generic;
using System.Text;
using XbmcScout.Models;

namespace XbmcScout.Providers {
    public interface IMovieMetadataProvider : IMetadataProvider {

        MovieXML Get(string MovieID);
        IVideo[] Search(string name);
    }
}
