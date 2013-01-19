using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace MediaScout
{
    [XmlRoot("Title")]
    public class MovieXML
    {
        #region Serialize Properties
        
        [XmlIgnore]
        private String localtitle;
        public String LocalTitle
        {
            get { return localtitle; }
            set { localtitle = value; }
        }
        
        public String OriginalTitle;
        public String SortTitle;
        public String RunningTime;
        public String IMDBrating;
        public String ProductionYear;

        [XmlElement("TMDbId")]
        public String ID;

        [XmlIgnore]
        private String description;
        public String Description
        {
            get { return description; }
            set { description = value; }
        }

        public List<Person> Persons = new List<Person>();
        public List<Genre> Genres = new List<Genre>();

        #endregion

        #region Non Serialze Properties

        [XmlIgnore]
        public bool LoadedFromCache;

        [XmlIgnore]
        public String Rating
        {
            get { return IMDBrating; }
            set { IMDBrating = value; }
        }

        [XmlIgnore]
        public String Year
        {
            get { return ProductionYear; }
            set { ProductionYear = value; }
        }

        [XmlIgnore]
        public String Length
        {
            get { return RunningTime; }
            set { RunningTime = value; }
        }

        [XmlIgnore]
        public String TMDbId
        {
            get { return ID; }
            set { ID = value; }
        }

        [XmlIgnore]
        public String posterthumb;

        [XmlIgnore]
        public String PosterThumb
        {
            get { return posterthumb; }
            set { posterthumb = value; }
        }
        
        [XmlIgnore]
        public String Title
        {
            get { return OriginalTitle; }
            set { OriginalTitle = value; }
        }

        [XmlIgnore]
        public String Tagline;

        [XmlIgnore]
        public String MPAA;

        #endregion
        
        #region Get And Save File Routines

        public String GetXBMCThumbFilename(String FileName)
        {
            return (FileName + ".tbn");
        }
        public String GetXBMCThumbFile(String Directory, String FileName)
        {
            return (Directory + "\\" + GetXBMCThumbFilename(FileName));
        }

        public String GetXBMCBackdropFilename(String FileName)
        {
            return (FileName + "_fanart.jpg");
        }
        public String GetXBMCBackdropFile(String Directory, String FileName)
        {
            return (Directory + "\\" + GetXBMCBackdropFilename(FileName));
        }

        public String GetXMLFilename()
        {
            return ("mymovies.xml");
        }
        public String GetNFOFilename()
        {
            return ("movie.nfo");
        }

        public String GetXMLFile(String Directory)
        {
            return (Directory + "\\" + GetXMLFilename());
        }
        public String GetNFOFile(String Directory)
        {
            return (Directory + "\\" + GetNFOFilename());
        }
        
        public void SaveXML(String FolderPath)
        {
            String FileName = GetXMLFile(FolderPath); ;
            XmlSerializer s = new XmlSerializer(typeof(MovieXML));
            TextWriter w = new StreamWriter(FileName);
            s.Serialize(w, this);
            w.Close();
        }
        public void SaveNFO(String FolderPath)
        {
            MovieNFO mNFO = new MovieNFO();
            mNFO.title = this.Title;
            mNFO.rating = this.Rating;
            mNFO.year = this.Year;
            mNFO.plot = this.Description;
            mNFO.tagline = this.Tagline;
            mNFO.runtime = this.Length;
            mNFO.mpaa = this.MPAA;
            mNFO.id = this.ID;

            if (this.Genres.Count > 0)
            {
                mNFO.genre = this.Genres[0].name;
                for (int i = 1; i < this.Genres.Count; i++)
                    mNFO.genre += " / " + this.Genres[i].name;
            }

            foreach (Person p in this.Persons)
                if (p.Type == "Director")
                {
                    if (mNFO.director == null)
                        mNFO.director = p.Name;
                    else
                        mNFO.director += " / " + p.Name;
                }

            foreach (Person p in this.Persons)
                if (p.Type == "Writer")
                {
                    if (mNFO.credits == null)
                        mNFO.credits = p.Name;
                    else
                        mNFO.credits += " / " + p.Name;
                }

            foreach (Person p in this.Persons)
                if (p.Type == "Actor")
                    mNFO.Actors.Add(new ActorsNFO()
                    {
                        name = p.Name,
                        role = p.Role,
                        thumb = p.Thumb
                    });
            mNFO.Save(GetNFOFile(FolderPath));
        }
        
        #endregion

        public override String ToString()
        {
            return this.Title;
        }
    }
}
