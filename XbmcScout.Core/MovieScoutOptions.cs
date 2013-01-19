using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class MovieScoutOptions {
        public bool SaveXBMCMeta;
        public bool SaveMyMoviesMeta;

        public bool GetMoviePosters;
        public bool GetMovieFilePosters;
        public bool MoveFiles;

        public bool DownloadAllPosters;
        public bool DownloadAllBackdrops;

        public bool RenameFiles;
        public String FileRenameFormat;
        public String DirRenameFormat;

        public String[] AllowedFileTypes;
        public String[] AllowedSubtitles;
        public bool ForceUpdate;

        public bool overwrite;

        public bool SaveActors;
    }
}