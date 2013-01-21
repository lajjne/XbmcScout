using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class TVScoutOptions {
        public bool SaveXBMCMeta;

        public bool GetSeriesPosters;
        public bool GetSeasonPosters;
        public bool GetEpisodePosters;

        public bool DownloadAllPosters;
        public bool DownloadAllBackdrops;
        public bool DownloadAllBanners;
        public bool DownloadAllSeasonPosters;
        public bool DownloadAllSeasonBackdrops;


        public String[] AllowedFileTypes = {".avi",".mkv",".mp4",".mpg",".mpeg",".ogm",".wmv",".divx",".dvr-ms"};

        public bool Overwrite;

        public bool SaveActors;
    }
}