using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Net;

namespace XbmcScout.Models {

    public interface IVideo {

        /// <summary>
        /// Gets the provider ID.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Gets the video title.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the year.
        /// </summary>
        string Year { get; }

    }
}
