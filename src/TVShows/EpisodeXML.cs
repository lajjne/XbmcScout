using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace MediaScout
{
    [XmlRoot("Item")]
    public class EpisodeXML
    {
        public Int32 ID;
        public String Director;
        public String EpisodeID;
        public String EpisodeName;
        public String EpisodeNumber;
        public String FirstAired;
        public String GuestStars;
        public String Language;
        public String Overview;
        public String ProductionCode;
        public String Writer;
        public String SeasonNumber;
        public String SeasonID;
        public String SeriesID;
        public String LastUpdated;

        [XmlIgnore]
        public String PosterUrl;

        [XmlElement("filename")]
        public String PosterName;

        [XmlIgnore]
        public System.Drawing.Image Poster;

        [XmlIgnore]
        public String BannerUrl;

        [XmlIgnore]
        public System.Drawing.Image Banner;

        [XmlIgnore]
        public String Rating;

        #region Save File Routines
        
        public String GetMetadataFolder(String Directory)
        {
            return (Directory + "\\metadata");
        }

        public String GetXBMCThumbFilename(String FileName)
        {
            return (FileName + ".tbn");
        }
        public String GetMyMoviesThumbFilename()
        {
            String File = null;
            if (PosterName != null)
                File = PosterName;
            else
                File = EpisodeID + ".jpg";
            return File;
        }

        public String GetXBMCThumbFile(String Directory, String FileName)
        {
            return (Directory + "\\" + GetXBMCThumbFilename(FileName));
        }
        public String GetMyMoviesThumbFile(String Directory)
        {
            return GetMetadataFolder(Directory) + "\\" + GetMyMoviesThumbFilename();
        }

        public String GetXMLFilename(String FileName)
        {
            return (GetMetadataFolder(FileName) + ".xml");
        }
        public String GetNFOFileName(String FileName)
        {
            return (FileName + ".nfo");
        }

        public String GetXMLFile(String Directory, String FileName)
        {
            return (GetMetadataFolder(Directory) + "\\" + GetXMLFilename(FileName));
        }
        public String GetNFOFile(String Directory, String FileName)
        {
            return (Directory + "\\" + GetNFOFileName(FileName));
        }

        public void SaveXML(String FolderPath, String Filename)
        {
            String FilePath = GetXMLFile(FolderPath, Filename);
            XmlSerializer s = new XmlSerializer(typeof(EpisodeXML));
            TextWriter w = new StreamWriter(FilePath);
            s.Serialize(w, this);
            w.Close();
        }
        public void SaveNFO(String FolderPath, String Filename)
        {
            EpisodeNFO eNFO = new EpisodeNFO();
            eNFO.title = this.EpisodeName;
            eNFO.rating = this.Rating;
            eNFO.season = this.SeasonNumber;
            eNFO.episode = this.EpisodeNumber;
            eNFO.plot = this.Overview;
            eNFO.aired = this.FirstAired;
            eNFO.credits = this.Writer;
            eNFO.director = this.Director;
           
            eNFO.Save(GetNFOFile(FolderPath, Filename));
        }

        #endregion

        
    }
}
