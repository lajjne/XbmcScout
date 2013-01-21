using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core.Providers {
    interface ITVMetadataProvider : IMetadataProvider {

        TVShowXML[] Search(string SeriesName);

        TVShowXML GetTVShow(string TVShowID);

        EpisodeXML GetEpisode(string TVShowID, string SeasonID, string EpisodeID);

    }
}
