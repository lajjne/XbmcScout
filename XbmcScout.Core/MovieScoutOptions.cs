using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class MovieScoutOptions {
        public bool SaveXBMCMeta = true;
        public bool SaveMyMoviesMeta;

        public bool GetMoviePosters = true;
        public bool GetMovieFilePosters = true; // backfrop/fanart
        public bool MoveFiles;

        public bool DownloadAllPosters;
        public bool DownloadAllBackdrops;

        public bool RenameFiles;
        public String FileRenameFormat;
        public String DirRenameFormat;

        public String[] AllowedFileTypes = {".avi",".mkv",".mp4",".mpg",".mpeg",".ogm",".wmv",".divx",".dvr-ms"};
        public String[] AllowedSubtitles = {".sub", ".idx;", ".srt"};

        public bool ForceUpdate;

        public bool overwrite;

        public bool SaveActors;
    }
}