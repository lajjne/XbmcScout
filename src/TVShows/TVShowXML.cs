using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace MediaScout
{
    [XmlRoot("Series")]
    public class TVShowXML
    {

        #region Serialize Properties

        public String id;
        public String Actors;
        public String ContentRating;
        public String FirstAired;
        public String Genre;
        public String IMDB_ID;
        public String Language;
        public String Network;
        public String Rating;
        public String Runtime;
        public String SeriesID;
        public String Status;

        [XmlIgnore]
        private String overview;
        public String Overview
        {
            get { return overview; }
            set { overview = value; }
        }

        [XmlIgnore]
        private String seriesName;
        public String SeriesName
        {
            get { return seriesName; }
            set { seriesName = value; }
        }
        
        #endregion

        #region Non Serialze Properties

        [XmlIgnore]
        public List<Person> Persons = new List<Person>();

        [XmlIgnore]
        public bool LoadedFromCache;

        [XmlIgnore]
        public SortedList<Int32, Season> Seasons = new SortedList<Int32, Season>();

        [XmlIgnore]
        public String posterthumb;

        [XmlIgnore]
        public String PosterThumb
        {
            get { return posterthumb; }
            set { posterthumb = value; }
        }
        
        [XmlIgnore]
        public String ID
        {
            get { return id; }
            set { id = value; }
        }
        
        [XmlIgnore]
        public String Title
        {
            get { return SeriesName; }
            set { SeriesName = value; }
        }

        [XmlIgnore]
        public String Description
        {
            get { return Overview; }
            set { Overview = value; }
        }

        [XmlIgnore]
        private String year = null;

        [XmlIgnore]
        public String Year
        {
            get { return year; }
            set { year = value; }
        }

        [XmlIgnore]
        public String Tagline;

        #endregion

        #region Save File Routines

        public String GetXMLFilename()
        {
            return ("series.xml");
        }
        public String GetNFOFileName()
        {
            return ("tvshow.nfo");
        }

        public String GetXMLFile(String Directory)
        {
            return (Directory + "\\" + GetXMLFilename());
        }
        public String GetNFOFile(String Directory)
        {
            return (Directory + "\\" + GetNFOFileName());
        }

        public void SaveXML(String Folderpath)
        {
            String FileName = Folderpath + "\\series.xml";
            XmlSerializer s = new XmlSerializer(typeof(TVShowXML));
            TextWriter w = new StreamWriter(FileName);
            s.Serialize(w, this);
            w.Close();
        }
        public void SaveNFO(String Folderpath)
        {
            TVShowNFO tsNFO = new TVShowNFO();
            tsNFO.title = this.Title;
            tsNFO.rating = this.Rating;
            tsNFO.year = this.Year;
            tsNFO.plot = this.Description;
            tsNFO.tagline = this.Tagline;
            tsNFO.runtime = this.Runtime;
            tsNFO.mpaa = this.ContentRating;
            tsNFO.id = this.ID;
            tsNFO.premiered = this.FirstAired;
            tsNFO.status = this.Status;
            tsNFO.studio = this.Network;
            tsNFO.genre = this.Genre;

            foreach (Person actor in this.Persons)
            {                
                tsNFO.Actors.Add(new ActorsNFO()
                {
                    name = actor.Name,
                    role = actor.Role,
                    thumb = actor.Thumb
                });
            }
            tsNFO.Save(GetNFOFile(Folderpath));
        }
        
        #endregion

        public override String ToString()
        {
            return this.Title;
        }
    }
}
