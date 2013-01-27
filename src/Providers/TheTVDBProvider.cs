using System;
using System.Linq;
using System.Collections.Generic;
using XbmcScout.Providers;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;
using System.IO;
using XbmcScout.Models;
using XbmcScout.Attributes;
using XbmcScout.Helpers;

namespace XbmcScout.Providers {
    public class TheTVDBProvider : ITVMetadataProvider {
        public Log _log;

        public TheTVDBProvider(Log logger) {
            this._log = logger;
        }

        #region IMetadataProvider Members

        public string Name { get { return "TheTVDB"; } }
        public string Version { get { return "2.1"; } }
        public string Url { get { return "http://www.thetvdb.com"; } }

        #endregion

        /// <summary>
        /// http://www.thetvdb.com/api/GetSeries.php?seriesname=Chuck
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/80348/all/en.xml
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/80348/actors.xml
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/80348/banners.xml
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/80348/default/1/1/en.xml
        /// </summary>
        private const String APIKey = "4AD667B666AA62FA";
        private String urlSeriesID = @"http://www.thetvdb.com/api/GetSeries.php?seriesname=";
        private String urlMetadata = @"http://www.thetvdb.com/api/" + APIKey + "/series/";
        private String urlPoster = @"http://thetvdb.com/banners/";
        private String defaultLanguage = "en";

        public String defaultCacheDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MediaScout\Cache\TVCache\";
        public DateTime dtDefaultCache = DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0));

        #region Search for TV Show

        public IVideo[] Search(String SeriesName) {
            return Search(SeriesName, defaultLanguage);
        }
        public IVideo[] Search(String SeriesName, String Language) {
            if (_log != null)
                _log(Level.Debug, "Querying TV ID for " + SeriesName);

            XmlDocument xdoc = new XmlDocument();
            XmlNode node;
            List<TVShowXML> tvshows = new List<TVShowXML>();

            try {
                xdoc.Load(urlSeriesID + SeriesName + "&language=" + Language);
                node = xdoc.DocumentElement;

                XmlNodeList xnl = node.SelectNodes("/Data/Series");
                for (int i = 0; i < xnl.Count; i++) {
                    TVShowXML t = new TVShowXML();
                    if (xnl[i]["seriesid"] != null)
                        t.SeriesID = xnl[i]["seriesid"].InnerText;

                    if (xnl[i]["SeriesName"] != null)
                        t.SeriesName = xnl[i]["SeriesName"].InnerText;

                    if (xnl[i]["Overview"] != null)
                        t.Overview = xnl[i]["Overview"].InnerText;

                    if (xnl[i]["id"] != null)
                        t.id = xnl[i]["id"].InnerText;

                    if (xnl[i]["banner"] != null)
                        t.PosterThumb = urlPoster + xnl[i]["banner"].InnerText;

                    if (xnl[i].SelectSingleNode("FirstAired") != null)
                        if (!String.IsNullOrEmpty(xnl[i].SelectSingleNode("FirstAired").InnerText))
                            t.Year = xnl[i].SelectSingleNode("FirstAired").InnerText.Substring(0, 4);

                    tvshows.Add(t);
                }



                if (tvshows.Count > 0)
                    return tvshows.Cast<IVideo>().ToArray();

            } catch (Exception ex) {
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return null;
        }

        #endregion

        #region Get TV Show Details

        public TVShowXML GetTVShow(String TVShowID) {
            return GetTVShow(TVShowID, defaultLanguage, defaultCacheDir, dtDefaultCache);
        }
        public TVShowXML GetTVShow(String TVShowID, DateTime dtCacheTime) {
            return GetTVShow(TVShowID, defaultLanguage, defaultCacheDir, dtCacheTime);
        }
        public TVShowXML GetTVShow(String TVShowID, String language) {
            return GetTVShow(TVShowID, language, defaultCacheDir, dtDefaultCache);
        }
        public TVShowXML GetTVShow(String TVShowID, String Language, String CacheDirectory, DateTime dtCacheTime) {

            XmlDocument xdoc = new XmlDocument();
            XmlNode node;
            XmlNodeList nodeList;
            TVShowXML s;

            if (CacheDirectory == null)
                CacheDirectory = defaultCacheDir;

            if (dtCacheTime == null)
                dtCacheTime = dtDefaultCache;

            if (Language == null)
                Language = defaultLanguage;

            try {
                s = new TVShowXML();
                if (File.Exists(CacheDirectory + "\\" + TVShowID + ".xml") && (DateTime.Compare(File.GetLastWriteTime(CacheDirectory + "\\" + TVShowID + ".xml"), dtCacheTime) > 0)) {
                    if (_log != null)
                        _log(Level.Debug, "Loading from cache");

                    xdoc.Load(CacheDirectory + "\\" + TVShowID + ".xml");
                    s.LoadedFromCache = true;
                } else {
                    if (_log != null)
                        _log(Level.Debug, "Fetching Metadata");
                    xdoc.Load(urlMetadata + TVShowID + "/all/" + Language + ".xml");
                    s.LoadedFromCache = false;
                }

                node = xdoc.DocumentElement;

                //Create Series/Fetch Series Metadata
                nodeList = node.SelectNodes("/Data/Series");
                s.SeriesID = s.ID = TVShowID;
                s.SeriesName = nodeList[0].SelectSingleNode("SeriesName").InnerText;

                s.PosterThumb = nodeList[0].SelectSingleNode("poster").InnerText;

                if (nodeList[0].SelectSingleNode("Network").InnerText != null)
                    s.Network = nodeList[0].SelectSingleNode("Network").InnerText;
                if (nodeList[0].SelectSingleNode("Rating").InnerText != null)
                    s.Rating = nodeList[0].SelectSingleNode("Rating").InnerText;
                if (nodeList[0].SelectSingleNode("Overview").InnerText != null)
                    s.Overview = nodeList[0].SelectSingleNode("Overview").InnerText;
                if (nodeList[0].SelectSingleNode("Runtime").InnerText != null)
                    s.Runtime = nodeList[0].SelectSingleNode("Runtime").InnerText;
                if (nodeList[0].SelectSingleNode("Genre").InnerText != null)
                    s.Genre = nodeList[0].SelectSingleNode("Genre").InnerText;
                if (nodeList[0].SelectSingleNode("FirstAired").InnerText != null)
                    s.FirstAired = nodeList[0].SelectSingleNode("FirstAired").InnerText;
                if (nodeList[0].SelectSingleNode("ContentRating").InnerText != null)
                    s.ContentRating = nodeList[0].SelectSingleNode("ContentRating").InnerText;
                if (nodeList[0].SelectSingleNode("Actors").InnerText != null)
                    s.Actors = nodeList[0].SelectSingleNode("Actors").InnerText;

                if (nodeList[0].SelectSingleNode("FirstAired") != null)
                    if (!String.IsNullOrEmpty(nodeList[0].SelectSingleNode("FirstAired").InnerText))
                        s.Year = nodeList[0].SelectSingleNode("FirstAired").InnerText.Substring(0, 4);

                //Deal with the XML for specific episodes
                nodeList = node.SelectNodes("/Data/Episode");

                s.Persons = GetActors(s.ID);

                foreach (XmlNode x in nodeList) {
                    //Extract metadata for episode/seasons
                    Int32 SeasonNumber = Int32.Parse(x["SeasonNumber"].InnerText);
                    Int32 EpisodeNumber = Int32.Parse(x["EpisodeNumber"].InnerText);
                    String EpisodePosterURL = x["filename"].InnerText;

                    if (!s.Seasons.ContainsKey(SeasonNumber)) {
                        s.Seasons.Add(SeasonNumber, new Season(SeasonNumber, TVShowID));
                    }

                    if (!s.Seasons[SeasonNumber].Episodes.ContainsKey(EpisodeNumber)) {
                        EpisodeXML ep = new EpisodeXML();
                        ep.ID = EpisodeNumber;
                        ep.EpisodeNumber = EpisodeNumber.ToString();
                        ep.EpisodeName = x["EpisodeName"].InnerText;
                        if (String.IsNullOrEmpty(EpisodePosterURL))
                            ep.PosterUrl = "";
                        else {
                            ep.PosterUrl = urlPoster + EpisodePosterURL;
                            ep.PosterName = EpisodePosterURL.Substring(EpisodePosterURL.LastIndexOf("/") + 1);
                        }
                        ep.FirstAired = x["FirstAired"].InnerText;
                        ep.ProductionCode = x["ProductionCode"].InnerText;
                        ep.Overview = x["Overview"].InnerText; ;
                        ep.EpisodeID = x["id"].InnerText;
                        ep.SeasonNumber = SeasonNumber.ToString();
                        ep.GuestStars = x["GuestStars"].InnerText;
                        ep.Director = x["Director"].InnerText;
                        ep.Writer = x["Writer"].InnerText;
                        ep.Rating = x["Rating"].InnerText;
                        s.Seasons[SeasonNumber].Episodes.Add(EpisodeNumber, ep);
                    }
                }



                //Cache metadata
                if ((!s.LoadedFromCache) || (!File.Exists(CacheDirectory + "\\" + TVShowID + ".xml"))) {
                    if (_log != null)
                        _log(Level.Debug, "Caching Metadata");
                    xdoc.Save(CacheDirectory + "\\" + TVShowID + ".xml");

                }


            } catch (Exception ex) {
                s = null;
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return s;

        }

        #endregion

        #region Get Episode Details

        public EpisodeXML GetEpisode(String TVShowID, String SeasonID, String EpisodeID) {
            XmlDocument xdoc = new XmlDocument();
            XmlNode node;
            XmlNodeList nodeList;
            EpisodeXML e = new EpisodeXML();

            String Language = defaultLanguage;

            try {
                if (_log != null)
                    _log(Level.Debug, "Fetching Episode Metadata");

                xdoc.Load(urlMetadata + TVShowID + "/default/" + SeasonID + "/" + EpisodeID + "/" + Language + ".xml");
                node = xdoc.DocumentElement;

                //Create Series/Fetch Episode Metadata
                nodeList = node.SelectNodes("/Data/Episode");
                e.ID = int.Parse(EpisodeID);
                e.SeriesID = nodeList[0].SelectSingleNode("id").InnerText;
                e.EpisodeName = nodeList[0].SelectSingleNode("EpisodeName").InnerText;

                e.SeasonID = nodeList[0].SelectSingleNode("seasonid").InnerText;
                e.EpisodeNumber = nodeList[0].SelectSingleNode("EpisodeNumber").InnerText;
                e.FirstAired = nodeList[0].SelectSingleNode("FirstAired").InnerText;
                e.GuestStars = nodeList[0].SelectSingleNode("GuestStars").InnerText;
                e.Director = nodeList[0].SelectSingleNode("Director").InnerText;
                e.Writer = nodeList[0].SelectSingleNode("Writer").InnerText;
                e.Rating = nodeList[0].SelectSingleNode("Rating").InnerText;
                e.Overview = nodeList[0].SelectSingleNode("Overview").InnerText;
                e.ProductionCode = nodeList[0].SelectSingleNode("ProductionCode").InnerText;
                e.LastUpdated = nodeList[0].SelectSingleNode("lastupdated").InnerText;
                String EpisodePosterUrl = nodeList[0].SelectSingleNode("filename").InnerText;
                if (String.IsNullOrEmpty(EpisodePosterUrl))
                    e.PosterUrl = "";
                else {
                    e.PosterUrl = urlPoster + EpisodePosterUrl;
                    e.PosterName = EpisodePosterUrl.Substring(EpisodePosterUrl.LastIndexOf("/") + 1);
                }
                e.SeriesID = nodeList[0].SelectSingleNode("seriesid").InnerText;
                e.SeasonNumber = nodeList[0].SelectSingleNode("SeasonNumber").InnerText;

            } catch (Exception ex) {
                e = null;
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return e;
        }

        #endregion

        #region Get Images for TV Show
        /// <summary>
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/79384/banners.xml
        /// </summary>
        /// <param name="TVShowID"></param>
        /// <param name="type"></param>
        /// <param name="seasonNum"></param>
        /// <returns></returns>
        public Posters[] GetPosters(String TVShowID, TVShowPosterType type, String seasonNum) {
            try {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(urlMetadata + TVShowID + "/banners.xml");
                XmlNodeList xnl = xdoc.DocumentElement.SelectNodes("/Banners/Banner");

                //List<String> posters = new List<string>();
                List<Posters> posters = new List<Posters>();

                String t = StringEnum.GetStringValue(type);
                foreach (XmlNode x in xnl) {
                    if (x.SelectSingleNode("BannerType").InnerText == t) {
                        if (type == TVShowPosterType.Season_Poster) {
                            if (x.SelectSingleNode("Season").InnerText != seasonNum)
                                continue;
                        }

                        Posters p = new Posters() {
                            Poster = urlPoster + x.SelectSingleNode("BannerPath").InnerText,
                            Thumb = (x.SelectSingleNode("ThumbnailPath") != null) ? urlPoster + x.SelectSingleNode("ThumbnailPath").InnerText : urlPoster + x.SelectSingleNode("BannerPath").InnerText,
                            Resolution = (x.SelectSingleNode("BannerType2") != null) ? x.SelectSingleNode("BannerType2").InnerText : null
                        };

                        posters.Add(p);
                    }
                }


                if (posters.Count > 0)
                    return posters.ToArray();
            } catch (Exception ex) {
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return null;
        }

        #endregion

        #region Get Actors for TV Show
        /// <summary>
        /// http://www.thetvdb.com/api/4AD667B666AA62FA/series/80348/actors.xml
        /// </summary>
        /// <param name="TVShowID"></param>        
        /// <returns></returns>
        public List<Person> GetActors(String TVShowID) {
            List<Person> Actors = new List<Person>();

            try {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(urlMetadata + TVShowID + "/actors.xml");
                XmlNodeList xnl = xdoc.DocumentElement.SelectNodes("/Actors/Actor");


                foreach (XmlNode x in xnl) {
                    String Name = null;
                    String Role = null;
                    String Thumb = null;
                    if (x.SelectSingleNode("Name") != null)
                        Name = x.SelectSingleNode("Name").InnerText;
                    if (x.SelectSingleNode("Role") != null)
                        if (!String.IsNullOrEmpty(x.SelectSingleNode("Role").InnerText))
                            Role = x.SelectSingleNode("Role").InnerText;
                    if (x.SelectSingleNode("Image") != null)
                        if (!String.IsNullOrEmpty(x.SelectSingleNode("Image").InnerText))
                            Thumb = urlPoster + x.SelectSingleNode("Image").InnerText;
                    Person p = new Person() {
                        Name = Name,
                        Type = "Actor",
                        Role = Role,
                        Thumb = Thumb
                    };

                    Actors.Add(p);
                }

            } catch (Exception ex) {
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return Actors;
        }

        #endregion
    }

    public enum TVShowPosterType {
        [StringValue("poster")]
        Poster,

        [StringValue("fanart")]
        Backdrop,

        [StringValue("series")]
        Banner,

        [StringValue("season")]
        Season_Poster,

        [StringValue("fanart")]
        Season_Backdrop,

    }
}
