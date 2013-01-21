using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace XbmcScout {
    /// <summary>
    /// http://wiki.xbmc.org/index.php?title=Import-export_library#TV_Shows
    /// </summary>
    [XmlRoot("tvshow")]
    public class TVShowNFO {
        public String title;
        public String rating;
        public String year;
        public String top250;
        public String season;
        public String episode;
        public String votes;
        public String outline;
        public String plot;
        public String tagline;
        public String runtime;
        public String mpaa;
        public String id;
        public String set;
        public String aired;
        public String premiered {
            get { return aired; }
            set { aired = value; }
        }
        public String status;
        public String code;
        public String studio;
        public String genre = null;
        public String director = null;
        public String credits = null;

        [XmlElement("actor")]
        public List<ActorsNFO> Actors = new List<ActorsNFO>();

        public String trailer;

        public void Save(String FilePath) {
            XmlSerializer s = new XmlSerializer(typeof(TVShowNFO));
            TextWriter w = new StreamWriter(FilePath);
            s.Serialize(w, this);
            w.Close();
        }
    }
}
