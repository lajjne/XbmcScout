using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class TVScoutOptions {
        public bool SaveXBMCMeta;
        public bool SaveMyMoviesMeta;

        public bool GetSeriesPosters;
        public bool GetSeasonPosters;
        public bool GetEpisodePosters;
        public bool MoveFiles;

        public String SeasonFolderName;
        public String SpecialsFolderName;
        public bool DownloadAllPosters;
        public bool DownloadAllBackdrops;
        public bool DownloadAllBanners;
        public bool DownloadAllSeasonPosters;
        public bool DownloadAllSeasonBackdrops;

        public bool RenameFiles;
        public String RenameFormat;
        public int SeasonNumZeroPadding;
        public int EpisodeNumZeroPadding;

        public String[] AllowedFileTypes;
        public String[] AllowedSubtitles;
        public bool ForceUpdate;

        public bool overwrite;

        public bool SaveActors;
    }
}