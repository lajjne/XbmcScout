using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Models {

    /// <summary>
    /// Gets or sets the options to use when downloading metadata and images.
    /// </summary>
    public class Flags {

        /// <summary>
        /// Get posters.
        /// </summary>
        public bool GetPosters = true;

        /// <summary>
        /// Get backdrop/fanart.
        /// </summary>
        public bool GetBackdrops = true;

        /// <summary>
        /// Overwrite existing metadata and images.
        /// </summary>
        public bool Overwrite;
    }
}