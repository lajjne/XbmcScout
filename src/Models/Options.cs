using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XbmcScout.Models {

    /// <summary>
    /// Gets or sets the options to use when downloading metadata and images.
    /// </summary>
    public class Options {

        /// <summary>
        /// TMDb api key
        /// </summary>
        public string TMDbApiKey = ConfigurationManager.AppSettings["TMDbApiKey"];

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
        public bool Overwrite = false;

        /// <summary>
        /// 
        /// </summary>
        public bool TvSearch = false;
    }
}