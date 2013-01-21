using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout {
    public class TVScoutOptions {

        public bool GetPoster;

        public bool GetBackdrop;

        public bool GetSeriesBanners;



        public String[] AllowedFileTypes = {".avi",".mkv",".mp4",".mpg",".mpeg",".ogm",".wmv",".divx",".dvr-ms"};

        public bool Overwrite;

    }
}