using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;

using XbmcScout.Providers;
using XbmcScout.Models;

namespace XbmcScout {
    public class TVScout {
        TheTVDBProvider tvdb;
        TVShowXML show;
        Log log;
        Options options;
        List<string> AllowedFileTypes = new List<string> {".avi",".mkv",".mp4",".mpg",".mpeg",".ogm",".wmv",".divx",".dvr-ms"};

        public TVScout(TVShowXML show, Options options, Log log) {
            this.show = show;
            this.options = options;
            this.log = log;
            tvdb = new TheTVDBProvider(log);
        }

        public String ProcessDirectory(String directory) {
            String name = null;

            SaveMeta(directory);

            if (options.GetPosters) {
                SaveImage(directory, "folder.jpg", null, 0, null, Providers.TVShowPosterType.Poster);
                SaveImage(directory, "banner.jpg", null, 0, null, Providers.TVShowPosterType.Banner);
            }

            if (options.GetBackdrops) {
                SaveImage(directory, "fanart.jpg", null, 0, null, Providers.TVShowPosterType.Backdrop);
            }

            // Process the season folders
            DirectoryInfo diShow = new DirectoryInfo(directory);
            foreach (DirectoryInfo diSeason in diShow.GetDirectories())
                ProcessSeasonDirectory(directory, diSeason);

            return name;
        }

        public String ProcessSeasonDirectory(String ShowDirectory, DirectoryInfo diSeason) {
            String name = diSeason.Name;

            Regex rSeasons = new Regex("Season.([0-9]+)", RegexOptions.IgnoreCase);
            MatchCollection mc = rSeasons.Matches(diSeason.Name);
            if (mc.Count > 0) {
                log(Level.Debug, "Processing " + diSeason.Name);

                //Using int to make "01" becoming "1" later
                int seasonNum = Int32.Parse(mc[0].Groups[1].Captures[0].Value);

                //FreQi - Make sure the discovered season number is valid (in the metadata from theTVDB.com)
                if (show.Seasons.ContainsKey(seasonNum)) {
                    String newName = "Season " + seasonNum.ToString();
                    ProcessSeason(ShowDirectory, newName, seasonNum);
                } else
                    log(Level.Warn, "Invalid Season folder:" + diSeason.Name);
            }
            return name;
        }

        public void ProcessSeason(String directory, String seasonFldr, int seasonNum) {
            log(Level.Info, "Valid");


            //Save the season poster, if there is one and we're supposed to
            if (options.GetPosters) {
                SaveImage(directory + "\\" + seasonFldr, "folder.jpg", null, 0, seasonNum.ToString(), Providers.TVShowPosterType.Season_Poster);
            }

            if (options.GetBackdrops) {
                SaveImage(directory + "\\" + seasonFldr, "fanart.jpg", null, 0, seasonNum.ToString(), Providers.TVShowPosterType.Season_Backdrop);
            }

            DirectoryInfo diEpisodes = new DirectoryInfo(String.Format("{0}\\{1}", directory, seasonFldr));

            foreach (FileInfo fi in diEpisodes.GetFiles())
                if (AllowedFileTypes.Contains(fi.Extension.ToLower()))
                    ProcessEpisode(directory, fi, seasonNum, true);

        }



        public class EpisodeInfo {
            public int SeasonID = -1;
            public int EpisodeID = -1;
        }
        public EpisodeInfo GetSeasonAndEpisodeIDFromFile(String FileName) {
            EpisodeInfo ei = new EpisodeInfo();
            //FreQi - We've got a video file, let's see if we can determine what episode it is

            //Let's look for a couple patterns...
            //Is "S##E##" or "#x##" somewhere in there ?
            Match m = Regex.Match(FileName, "S(?<se>[0-9]{1,2})E(?<ep>[0-9]{1,3})|(?<se>[0-9]{1,2})x(?<ep>[0-9]{1,3})", RegexOptions.IgnoreCase);
            if (m.Success) {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Is "S##?EP##" or S#?EP##?
            m = Regex.Match(FileName, "S(?<se>[0-9]{1,2}).EP(?<ep>[0-9]{1,3})", RegexOptions.IgnoreCase);
            if (m.Success) {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Does the file START WITH just "###" (SEE) or #### (SSEE) ? (if not found yet)
            m = Regex.Match(FileName, "^(?<se>[0-9]{1,2})(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success) {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Is it just the two digit episode number maybe?
            m = Regex.Match(FileName, "^(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success)
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);

            //Does the file NOT START WITH just "###" (SEE) or #### (SSEE) ? (if not found yet)
            m = Regex.Match(FileName, "(?<se>[0-9]{1,2})(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success) {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            return ei;
        }
        public String ProcessEpisode(String ShowDirectory, FileInfo fi, int seasonNum, bool IsInsideShowDir) {


            log(Level.Debug, "Processing File : " + fi.Name);

            try {
                EpisodeInfo ei = GetSeasonAndEpisodeIDFromFile(fi.Name);

                String SeasonID = (ei.SeasonID != -1) ? ei.SeasonID.ToString() : "Unable to extract";
                String EpisodeID = (ei.EpisodeID != -1) ? ei.EpisodeID.ToString() : "Unable to extract";

                log(Level.Info, "Season : " + SeasonID + ", Episode : " + EpisodeID);

                //So, did we find an episode number?
                if (ei.SeasonID != -1 && ei.EpisodeID != -1) {
                    //Do we know what season this file "thinks" it's belongs to and is it in the right folder?
                    if (ei.SeasonID != seasonNum)

                        if (show.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                            return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID);
                        else {
                            log(Level.Error, String.Format("Episode {0} Not Found In Season {1}", ei.EpisodeID, ei.SeasonID));
                            if (show.LoadedFromCache) {
                                //Information in cache may be old, refetch and check again
                                log(Level.Debug, "Updating Cache");
                                show = tvdb.GetTVShow(show.SeriesID, DateTime.Now);
                                if (show != null) {
                                    if (show.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                                        return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID);
                                    else
                                        log(Level.Error, "Invalid Episode, Skipping");
                                }
                            }
                        }
                }
            } catch (Exception ex) {
                log(Level.Error, ex.Message);
            }
            return null;
        }

        public String ProcessFile(String ShowDirectory, FileInfo file, int seasonID, int episodeID) {
            String name = file.Name;
            EpisodeXML episode = show.Seasons[seasonID].Episodes[episodeID];


            log(Level.Debug, "Saving Metadata as " + episode.GetNFOFileName(file.Name.Replace(file.Extension, "")));
            if (options.Overwrite || !File.Exists(episode.GetNFOFile(file.DirectoryName, file.Name.Replace(file.Extension, "")))) {
                episode.SaveNFO(file.DirectoryName, file.Name.Replace(file.Extension, ""));

            } else {
                log(Level.Info, "Already Exists, skipping");
            }

            return name;
        }

        private void SaveMeta(String directory) {
            try {
                //Save Movie NFO
                log(Level.Debug, "Saving Metadata as " + show.GetNFOFile(directory));
                if (options.Overwrite || !File.Exists(show.GetNFOFile(directory))) {
                    show.SaveNFO(directory);
                } else
                    log(Level.Info, "Already Exists, skipping");
            } catch (Exception ex) {
                log(Level.Warn, ex.Message);
            }
        }

        private void SaveImage(String directory, String filename, Posters[] images, int index, String SeasonNum, Providers.TVShowPosterType ptype) {
            log(Level.Debug, "Saving " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + " as " + filename);
            if (!File.Exists(directory + "\\" + filename) || options.Overwrite) {
                try {
                    if (images == null)
                        images = tvdb.GetPosters(show.ID, ptype, SeasonNum);
                    if (images != null) {
                        images[index].SavePoster(directory + "\\" + filename);
                    } else
                        log(Level.Warn, "No " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + "s Found");
                } catch (Exception ex) {
                    log(Level.Warn, ex.Message);
                }
            } else
                log(Level.Info, "Already Exists, skipping");
        }

    }
}
