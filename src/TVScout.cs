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

namespace XbmcScout {
    public class TVScout {
        MediaScoutMessage.Message Message;
        TVScoutOptions options;
        Providers.TheTVDBProvider tvdb;


        public TVShowXML series;

        int level = 1;

        public TVScout(TVScoutOptions options, MediaScoutMessage.Message Message) {
            this.options = options;
            this.Message = Message;
            tvdb = new XbmcScout.Providers.TheTVDBProvider(Message);
        }


        public String ProcessDirectory(String directory) {
            int level = this.level;
            String name = null;



            SaveMeta(directory, level);


            if (options.GetPoster) {
                SaveImage(directory, "folder.jpg", null, 0, null, Providers.TVShowPosterType.Poster, level);
            }

            if (options.GetBackdrop) {
                SaveImage(directory, "fanart.jpg", null, 0, null, Providers.TVShowPosterType.Backdrop, level);
            }


            if (options.GetSeriesBanners)
                SaveImage(directory, "banner.jpg", null, 0, null, Providers.TVShowPosterType.Banner, level);



            // Process the season folders

            DirectoryInfo diShow = new DirectoryInfo(directory);
            foreach (DirectoryInfo diSeason in diShow.GetDirectories())
                ProcessSeasonDirectory(directory, diSeason, level + 1);

            return name;
        }

        public String ProcessSeasonDirectory(String ShowDirectory, DirectoryInfo diSeason, int level) {
            String name = diSeason.Name;

            if (level == -1)
                level = this.level;

            Regex rSeasons = new Regex("Season.([0-9]+)", RegexOptions.IgnoreCase);
            MatchCollection mc = rSeasons.Matches(diSeason.Name);
            if (mc.Count > 0) {
                Message("Processing " + diSeason.Name, MediaScoutMessage.MessageType.Task, level);

                //Using int to make "01" becoming "1" later
                int seasonNum = Int32.Parse(mc[0].Groups[1].Captures[0].Value);

                //FreQi - Make sure the discovered season number is valid (in the metadata from theTVDB.com)
                if (series.Seasons.ContainsKey(seasonNum)) {
                    String newName = "Season " + seasonNum.ToString();
                    ProcessSeason(ShowDirectory, newName, seasonNum, level + 1);
                } else
                    Message("Invalid Season folder:" + diSeason.Name, MediaScoutMessage.MessageType.TaskError, level);
            }
            return name;
        }

        public void ProcessSeason(String directory, String seasonFldr, int seasonNum, int level) {
            Message("Valid", MediaScoutMessage.MessageType.TaskResult, level);


            //Save the season poster, if there is one and we're supposed to
            if (options.GetPoster) {
                SaveImage(directory + "\\" + seasonFldr, "folder.jpg", null, 0, seasonNum.ToString(), XbmcScout.Providers.TVShowPosterType.Season_Poster, level);
            }

            if (options.GetBackdrop) {
                SaveImage(directory + "\\" + seasonFldr, "fanart.jpg", null, 0, seasonNum.ToString(), XbmcScout.Providers.TVShowPosterType.Season_Backdrop, level);
            }

            List<String> filetypes = new List<String>(options.AllowedFileTypes);
            DirectoryInfo diEpisodes = new DirectoryInfo(String.Format("{0}\\{1}", directory, seasonFldr));

            foreach (FileInfo fi in diEpisodes.GetFiles())
                if (filetypes.Contains(fi.Extension.ToLower()))
                    ProcessEpisode(directory, fi, seasonNum, true, level + 1);

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
        public String ProcessEpisode(String ShowDirectory, FileInfo fi, int seasonNum, bool IsInsideShowDir, int level) {
            if (level == -1)
                level = this.level;



            Message("Processing File : " + fi.Name, MediaScoutMessage.MessageType.Task, level);

            try {
                EpisodeInfo ei = GetSeasonAndEpisodeIDFromFile(fi.Name);

                String SeasonID = (ei.SeasonID != -1) ? ei.SeasonID.ToString() : "Unable to extract";
                String EpisodeID = (ei.EpisodeID != -1) ? ei.EpisodeID.ToString() : "Unable to extract";

                Message("Season : " + SeasonID + ", Episode : " + EpisodeID, (ei.SeasonID != -1 && ei.EpisodeID != -1) ? MediaScoutMessage.MessageType.TaskResult : MediaScoutMessage.MessageType.TaskError, level);

                //So, did we find an episode number?
                if (ei.SeasonID != -1 && ei.EpisodeID != -1) {
                    //Do we know what season this file "thinks" it's belongs to and is it in the right folder?
                    if (ei.SeasonID != seasonNum)

                        if (series.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                            return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID, level + 1);
                        else {
                            Message(String.Format("Episode {0} Not Found In Season {1}", ei.EpisodeID, ei.SeasonID), MediaScoutMessage.MessageType.Error, level + 1);
                            if (series.LoadedFromCache) {
                                //Information in cache may be old, refetch and check again
                                Message("Updating Cache", MediaScoutMessage.MessageType.Task, level + 1);
                                series = tvdb.GetTVShow(series.SeriesID, DateTime.Now, level + 2);
                                if (series != null) {
                                    if (series.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                                        return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID, level + 1);
                                    else
                                        Message("Invalid Episode, Skipping", MediaScoutMessage.MessageType.Error, level + 1);
                                }
                            }
                        }
                }
            } catch (Exception ex) {
                Message(ex.Message, MediaScoutMessage.MessageType.Error, level);
            }
            return null;
        }

        public String ProcessFile(String ShowDirectory, FileInfo file, int seasonID, int episodeID, int level) {
            String name = file.Name;
            EpisodeXML episode = series.Seasons[seasonID].Episodes[episodeID];


            Message("Saving Metadata as " + episode.GetNFOFileName(file.Name.Replace(file.Extension, "")), MediaScoutMessage.MessageType.Task, level);
            if (options.Overwrite || !File.Exists(episode.GetNFOFile(file.DirectoryName, file.Name.Replace(file.Extension, "")))) {
                episode.SaveNFO(file.DirectoryName, file.Name.Replace(file.Extension, ""));
                Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
            } else {
                Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
            }

            return name;
        }

        private void SaveMeta(String directory, int level) {
            try {
                //Save Movie NFO
                Message("Saving Metadata as " + series.GetNFOFile(directory), MediaScoutMessage.MessageType.Task, level);
                if (options.Overwrite || !File.Exists(series.GetNFOFile(directory))) {
                    series.SaveNFO(directory);
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                } else
                    Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
            } catch (Exception ex) {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }
        }

        private void SaveImage(String directory, String filename, Posters[] images, int index, String SeasonNum, Providers.TVShowPosterType ptype, int level) {
            Message("Saving " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + " as " + filename, MediaScoutMessage.MessageType.Task, level);
            if (!File.Exists(directory + "\\" + filename) || options.Overwrite) {
                try {
                    if (images == null)
                        images = tvdb.GetPosters(series.ID, ptype, SeasonNum);
                    if (images != null) {
                        images[index].SavePoster(directory + "\\" + filename);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    } else
                        Message("No " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + "s Found", MediaScoutMessage.MessageType.TaskError, level);
                } catch (Exception ex) {
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                }
            } else
                Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
        }

    }
}
