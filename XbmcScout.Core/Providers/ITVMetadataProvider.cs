using System;
using System.Collections.Generic;
using System.Text;

namespace MediaScout.Providers
{
    interface ITVMetadataProvider : IMetadataProvider
    {
        TVShowXML[] Search(String SeriesName);
        TVShowXML GetTVShow(String TVShowID);
        EpisodeXML GetEpisode(String TVShowID, String SeasonID, String EpisodeID);

    }
}
