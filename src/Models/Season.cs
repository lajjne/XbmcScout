using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Net;
namespace XbmcScout.Models {
    public class Season {
        public SortedList<Int32, EpisodeXML> Episodes = new SortedList<int, EpisodeXML>();
        public Int32 ID;
        private String TVShowID;

        public Season(Int32 seasonNumber, String TVShowID) {
            ID = seasonNumber;
            this.TVShowID = TVShowID;
        }

    }
}
