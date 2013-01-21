using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using XbmcScout.Providers;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using XbmcScout.Models;
using XbmcScout.Attributes;
using XbmcScout.Helpers;

namespace XbmcScout.Providers {

    public class TheMovieDBProvider : IMovieMetadataProvider {
        public MediaScoutMessage.Message Message;
        int level = 0;

        public TheMovieDBProvider(MediaScoutMessage.Message Message) {
            this.Message = Message;
        }

        string IMetadataProvider.Name { get { return "The Movie DB"; } }
        string IMetadataProvider.Version { get { return "2.1"; } }
        string IMetadataProvider.Url { get { return ""; } }

        /// <summary>
        /// http://api.themoviedb.org/2.1/Movie.search/en/xml/1a9efd23fff9c2ed07c90358e2b3d280/Transformers
        /// http://api.themoviedb.org/2.1/Movie.getInfo/en/xml/1a9efd23fff9c2ed07c90358e2b3d280/1858
        /// </summary>
        private static String APIKey = "1a9efd23fff9c2ed07c90358e2b3d280";
        private static String osUri = "http://a9.com/-/spec/opensearch/1.1/";
        private String urlSearchMovie = String.Format("http://api.themoviedb.org/2.1/Movie.search/{0}/xml/{1}/", "en", APIKey);
        private String urlMovieInfo = String.Format("http://api.themoviedb.org/2.1/Movie.getInfo/{0}/xml/{1}/", "en", APIKey);

        private String defaultCacheDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MediaScout\Cache\MovieCache\";
        private DateTime dtDefaultCache = DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0)); //Refresh Interval 14 days


        #region Search for a movie
        public IVideo[] Search(string MovieName) {
            if (Message != null)
                Message("Querying Movie ID for " + MovieName, MediaScoutMessage.MessageType.Task, level);

            //TheMovieDB doesn't handle & very well, so convert to "AND"
            MovieName = MovieName.Replace("&", "and");
            MovieName = MovieName.Replace(" ", "+");

            XmlDocument xdoc = new XmlDocument();
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
            nsMgr.AddNamespace("opensearch", osUri);
            List<MovieXML> movies = new List<MovieXML>();
            try {
                xdoc.Load(urlSearchMovie + MovieName);
                XmlNode node = xdoc.DocumentElement;
                XmlNodeList xnl = node.SelectNodes("./movies/movie");

                for (int i = 0; i < xnl.Count; i++) {
                    MovieXML m = new MovieXML();
                    if (xnl[i]["name"] != null)
                        m.Title = xnl[i]["name"].InnerText;

                    if (xnl[i]["alternative_name"] != null)
                        m.LocalTitle = xnl[i]["alternative_name"].InnerText;

                    if (xnl[i]["id"] != null)
                        m.ID = xnl[i]["id"].InnerText;

                    if (xnl[i]["overview"] != null)
                        m.Description = xnl[i]["overview"].InnerText;

                    if (xnl[i].SelectSingleNode("./images/image[@type='poster'][@size='thumb']") != null)
                        m.PosterThumb = xnl[i].SelectSingleNode("./images/image[@type='poster'][@size='thumb']").Attributes["url"].Value;

                    if (xnl[i].SelectSingleNode("released") != null) {
                        if (!String.IsNullOrEmpty(xnl[i].SelectSingleNode("released").InnerText))
                            m.ProductionYear = xnl[i].SelectSingleNode("released").InnerText.Substring(0, 4);
                    }

                    movies.Add(m);
                }

                if (Message != null)
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);

                if (movies.Count > 0)
                    return movies.Cast<IVideo>().ToArray();

            } catch (Exception ex) {
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }

            return null;
        }
        #endregion

        #region Get the Details of Movie

        public MovieXML Get(string MovieID) {
            return Get(MovieID, dtDefaultCache);
        }
        public MovieXML Get(string MovieID, DateTime dtCacheTime) {
            XmlNode node;
            XmlDocument xdoc = new XmlDocument();
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
            nsMgr.AddNamespace("opensearch", osUri);

            MovieXML m;
            try {
                m = new MovieXML();
                if (File.Exists(defaultCacheDir + "\\" + MovieID + ".xml") && (DateTime.Compare(File.GetLastWriteTime(defaultCacheDir + "\\" + MovieID + ".xml"), dtCacheTime) > 0)) {
                    if (Message != null)
                        Message("Loading from cache", MediaScoutMessage.MessageType.Task, level);

                    xdoc.Load(defaultCacheDir + "\\" + MovieID + ".xml");
                    m.LoadedFromCache = true;
                } else {
                    if (Message != null)
                        Message("Fetching Metadata", MediaScoutMessage.MessageType.Task, level);
                    xdoc.Load(urlMovieInfo + MovieID);
                    m.LoadedFromCache = false;
                }

                node = xdoc.DocumentElement;
                XmlNodeList nlMovie = node.SelectNodes("./movies/movie");

                if (nlMovie[0].FirstChild == null)
                    throw new Exception("no results");

                if (nlMovie[0].SelectSingleNode("released") != null)
                    if (!String.IsNullOrEmpty(nlMovie[0].SelectSingleNode("released").InnerText))
                        m.ProductionYear = nlMovie[0].SelectSingleNode("released").InnerText.Substring(0, 4);

                if (nlMovie[0].SelectSingleNode("overview") != null)
                    m.Description = nlMovie[0].SelectSingleNode("overview").InnerText;

                if (nlMovie[0].SelectSingleNode("runtime") != null)
                    m.RunningTime = nlMovie[0].SelectSingleNode("runtime").InnerText;

                if (nlMovie[0].SelectSingleNode("rating") != null)
                    m.Rating = nlMovie[0].SelectSingleNode("rating").InnerText;

                if (nlMovie[0].SelectSingleNode("tagline") != null)
                    m.Tagline = nlMovie[0].SelectSingleNode("tagline").InnerText;

                if (nlMovie[0].SelectSingleNode("certification") != null)
                    m.MPAA = nlMovie[0].SelectSingleNode("certification").InnerText;

                if (nlMovie[0].SelectSingleNode("alternative_name") != null)
                    m.LocalTitle = nlMovie[0].SelectSingleNode("alternative_name").InnerText;

                m.Title = nlMovie[0].SelectSingleNode("name").InnerText;
                m.ID = (nlMovie[0].SelectSingleNode("id") == null) ? nlMovie[0].SelectSingleNode("TMDbId").InnerText : nlMovie[0].SelectSingleNode("id").InnerText;

                //Get the actors/directors/etc
                XmlNodeList nlActors = node.SelectNodes("./movies/movie/cast/person");
                foreach (XmlNode x in nlActors) {
                    m.Persons.Add(new Person() {
                        ID = x.Attributes["id"].Value,
                        Name = x.Attributes["name"].Value,
                        Type = x.Attributes["job"].Value,
                        Role = (x.Attributes["name"].Value == null) ? "" : x.Attributes["character"].Value.Trim(),
                        Thumb = x.Attributes["thumb"].Value
                    });
                }

                //Get the genres
                XmlNodeList nlGenres = node.SelectNodes("./movies/movie/categories/category");
                foreach (XmlNode x in nlGenres)
                    m.Genres.Add(new Genre() { name = x.Attributes["name"].Value });

                if (Message != null)
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);

                if ((!File.Exists(defaultCacheDir + "\\" + MovieID + ".xml"))) {
                    //Cache metadata
                    if (Message != null)
                        Message("Caching Metadata", MediaScoutMessage.MessageType.Task, level);

                    if (!Directory.Exists(defaultCacheDir)) {
                        Directory.CreateDirectory(defaultCacheDir);
                    }
                    xdoc.Save(defaultCacheDir + "\\" + MovieID + ".xml");

                    if (Message != null)
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                }

            } catch (Exception ex) {
                m = null;
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }

            return m;

        }

        #endregion

        #region Get Movie Images
        public Posters[] GetPosters(String MovieID, MoviePosterType type) {
            //if (Message != null)
            //    Message("Getting " + type.ToString() + " List", MediaScoutMessage.MessageType.Task, level);

            try {
                string selectImages = string.Format("./images/image[@type='{0}'][@size='original']", StringEnum.GetStringValue(type));
                string selectImageThumbnail = string.Format("./images/image[@type='{0}'][@size='thumb']", StringEnum.GetStringValue(type));

                XmlDocument xdoc = new XmlDocument();
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
                nsMgr.AddNamespace("opensearch", osUri);
                xdoc.Load(urlMovieInfo + MovieID);

                XmlNodeList nlMovie = xdoc.DocumentElement.SelectNodes("./movies/movie");

                List<Posters> posters = new List<Posters>();

                XmlNodeList xnl = nlMovie[0].SelectNodes(selectImages);

                foreach (XmlNode x in xnl) {
                    string posterUrl = x.Attributes["url"].Value;
                    String res = x.Attributes["width"].Value + "x" + x.Attributes["height"].Value;
                    string thumbnailUrl = nlMovie[0].SelectSingleNode(string.Format(selectImageThumbnail + "[@id='{0}']", x.Attributes["id"].Value)).Attributes["url"].Value;
                    Posters p = new Posters() {
                        Poster = posterUrl,
                        Thumb = thumbnailUrl,
                        Resolution = res
                    };
                    posters.Add(p);
                }

                //if (Message != null)
                //    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);

                if (posters.Count > 0)
                    return posters.ToArray();
            } catch (Exception ex) {
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }
            return null;
        }
        #endregion

        #region Get Actors for Movie
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MovieID"></param>
        /// <returns></returns>
        public List<Person> GetActors(String MovieID) {
            List<Person> Actors = new List<Person>();

            try {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(urlMovieInfo + MovieID);
                XmlNodeList xnl = xdoc.DocumentElement.SelectNodes("./movies/movie/cast/person");

                foreach (XmlNode x in xnl) {
                    if (x.Attributes["job"].Value == "Actor") {
                        Person p = new Person() {
                            ID = x.Attributes["id"].Value,
                            Name = x.Attributes["name"].Value,
                            Type = x.Attributes["job"].Value,
                            Role = (x.Attributes["name"].Value == null) ? "" : x.Attributes["character"].Value.Trim(),
                            Thumb = x.Attributes["thumb"].Value,
                        };
                        Actors.Add(p);
                    }

                }

            } catch (Exception ex) {
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }

            return Actors;
        }

        #endregion

        #region Search for Person
        private String urlSearchPerson = String.Format("http://api.themoviedb.org/2.1/Person.search/{0}/xml/{1}/", "en", APIKey);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ActorsID"></param>
        /// <returns></returns>
        public List<Person> SearchPerson(String Name) {
            List<Person> Persons = new List<Person>();

            try {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(urlSearchPerson + Name);
                XmlNodeList xnl = xdoc.DocumentElement.SelectNodes("./people/person");

                foreach (XmlNode x in xnl) {
                    String strID = null;
                    String strName = null;
                    String strThumb = null;
                    if (x.SelectSingleNode("id") != null)
                        strID = x.SelectSingleNode("id").InnerText;
                    if (x.SelectSingleNode("name") != null)
                        strName = x.SelectSingleNode("name").InnerText;
                    if (x.SelectSingleNode("./images/image[@size='thumb']") != null)
                        strThumb = x.SelectSingleNode("./images/image[@size='thumb']").Attributes["url"].Value;
                    Person p = new Person() {
                        ID = strID,
                        Name = strName,
                        Thumb = strThumb
                    };

                    Persons.Add(p);
                }
            } catch (Exception ex) {
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }

            return Persons;
        }

        #endregion

        #region Get Person Images
        private String urlPersonInfo = String.Format("http://api.themoviedb.org/2.1/Person.getInfo/{0}/xml/{1}/", "en", APIKey);

        /// <summary>
        /// http://api.themoviedb.org/2.1/Person.getInfo/en/xml/1a9efd23fff9c2ed07c90358e2b3d280/59192
        /// </summary>
        /// <param name="ActorsID"></param>
        /// <returns></returns>
        public List<Posters> GetPersonImage(String PersonID) {
            List<Posters> ActorImages = new List<Posters>();

            try {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(urlPersonInfo + PersonID);

                XmlNodeList nlPerson = xdoc.DocumentElement.SelectNodes("./people/person");

                String images = "./images/image[@size='original']";
                String imageThumb = "./images/image[@size='original']";

                XmlNodeList xnl = nlPerson[0].SelectNodes(images);

                foreach (XmlNode x in xnl) {
                    String posterUrl = x.Attributes["url"].Value;
                    String ThumbUrl = nlPerson[0].SelectSingleNode(String.Format(imageThumb + "[@id='{0}']", x.Attributes["id"].Value)).Attributes["url"].Value;
                    Posters p = new Posters() {
                        Poster = posterUrl,
                        Thumb = ThumbUrl,
                        Resolution = x.Attributes["width"].Value + "x" + x.Attributes["height"].Value
                    };

                    ActorImages.Add(p);
                }

            } catch (Exception ex) {
                if (Message != null)
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }

            return ActorImages;
        }

        #endregion
    }

    public enum MoviePosterType {
        [StringValue("poster")]
        Poster,

        [StringValue("backdrop")]
        Backdrop,

        [StringValue("poster")]
        File_Poster,

        [StringValue("backdrop")]
        File_Backdrop,

    }
}
