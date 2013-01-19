using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;


namespace XbmcScout.Core {
    [XmlRoot("episodedetails")]
    public class EpisodeNFO {
        public String title;
        public String rating;
        public String season;
        public String episode;
        public String plot;
        public String director = null;
        public String credits = null;
        public String aired;

        [XmlElement("actor")]
        public List<ActorsNFO> Actors = new List<ActorsNFO>();

        public void Save(String FilePath) {
            XmlSerializer s = new XmlSerializer(typeof(EpisodeNFO));
            TextWriter w = new StreamWriter(FilePath);
            s.Serialize(w, this);
            w.Close();
        }
    }
}
