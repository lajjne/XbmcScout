using System;
using System.Collections.Generic;
using System.Text;
using XbmcScout.Models;

namespace XbmcScout.Providers {
    interface ITVMetadataProvider : IMetadataProvider {

        IVideo[] Search(string SeriesName);

        TVShowXML GetTVShow(string TVShowID);

        EpisodeXML GetEpisode(string TVShowID, string SeasonID, string EpisodeID);

    }
}
