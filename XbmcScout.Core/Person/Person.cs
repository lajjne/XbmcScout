using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace XbmcScout.Core {
    public class Person {
        public String Name;
        public String Type;
        public String Role;

        [XmlIgnore]
        public String Thumb;

        [XmlIgnore]
        public String ID; //Supported by TMDB only

        #region Save Image Functions

        public String GetXBMCFilename() {
            return Name.Replace(" ", "_") + ".jpg";
        }
        public String GetXBMCDirectory() {
            return ".actors";
        }

        public String GetMyMoviesFilename() {
            return "folder.jpg";
        }
        public String GetMyMoviesDirectory() {
            return Name;
        }

        public void SaveThumb(String Filepath) {
            Posters actor = new Posters() {
                Poster = Thumb
            };

            actor.SavePoster(Filepath);
        }

        #endregion
    }
}
