using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout {
    public class MovieScoutOptions {

        /// <summary>
        /// Get posters
        /// </summary>
        public bool GetPoster = true;

        /// <summary>
        /// Get backdrops
        /// </summary>
        public bool GetBackdrop = true; // backdrop/fanart

        /// <summary>
        /// Overwrite existing information metadata and images.
        /// </summary>
        public bool Overwrite;

    }
}