using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class MovieScoutOptions {
        public bool SaveXBMCMeta = true;

        public bool GetPosters = true;
        public bool GetBackdrop = true; // backfrop/fanart
        public bool GetActors;

        public bool DownloadAllPosters;
        public bool DownloadAllBackdrops;


        

        public bool Overwrite;

    }
}