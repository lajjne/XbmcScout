using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace XbmcScout.Core {
    /// <summary>
    /// See http://wiki.xbmc.org/index.php?title=Import-export_library#Movies
    /// </summary>
    [XmlRoot("movie")]
    public class MovieNFO {
        

        public String title {
            get { return localtitle; }
            set { localtitle = originaltitle = sorttitle = value; }
        }
        private String originaltitle;
        private String sorttitle;
        public String set;
        public String rating;
        public String year;
        public String top250;
        public String votes;
        public String outline;
        public String plot;
        public String tagline;
        public String runtime;
        public String mpaa;
        public String id;
        public String genre = null;
        public String director = null;
        public String credits = null;

        [XmlElement("actor")]
        public List<ActorsNFO> Actors = new List<ActorsNFO>();

        public String trailer;

        [XmlIgnore]
        private string localtitle;

        public void Save(String FilePath) {
            XmlSerializer s = new XmlSerializer(typeof(MovieNFO));
            TextWriter w = new StreamWriter(FilePath);
            s.Serialize(w, this);
            w.Close();
        }
    }
}
