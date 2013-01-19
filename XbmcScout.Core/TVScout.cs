using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;

using MediaScout.Providers;

namespace MediaScout
{
    public class TVScout
    {
        MediaScoutMessage.Message Message;        
        TVScoutOptions options;
        Providers.TheTVDBProvider tvdb;
        String ImagesByNameLocation = null;
        DirFunc mdf = new DirFunc();
        MoveFileFunc mff = new MoveFileFunc();

        public TVShowXML series;

        int level = 1;

        public TVScout(TVScoutOptions options, MediaScoutMessage.Message Message, String ImagesByNameLocation)
        {            
            this.options = options;
            this.Message = Message;
            this.ImagesByNameLocation = ImagesByNameLocation;
            tvdb = new MediaScout.Providers.TheTVDBProvider(Message);            
        }

        #region Process TVShow Directory
        
        public String ProcessDirectory(String directory)
        {
            int level = this.level;
            String name = null;

            #region Rename Directory
            name = mdf.GetDirectoryName(directory);
            if (options.RenameFiles)
            {
                if (series.SeriesName != name)
                {
                    String newPath = directory.Replace(name, "") + series.SeriesName;
                    if (mdf.MergeDirectories(directory, newPath, options.overwrite))
                        name = "d:" + series.SeriesName;
                    else
                        name = series.SeriesName;
                    directory = newPath;
                }
            }
            #endregion

            SaveMeta(directory, level);

            #region Process Images

            #region Save Series Poster
            
            if (options.GetSeriesPosters)
                SaveImage(directory, "folder.jpg", null, 0, null, Providers.TVShowPosterType.Poster, level);

            if (options.DownloadAllPosters)
            {
                Posters[] posters = tvdb.GetPosters(series.id, MediaScout.Providers.TVShowPosterType.Poster, null);

                Message("Downloading All Posters", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in posters)
                {
                    SaveImage(directory, "folder" + i + ".jpg", posters, i, null, Providers.TVShowPosterType.Poster, level+1);
                    i++;
                }
            }

            #endregion

            #region Save Series Backdrop

            if (options.GetSeriesPosters)
            {
                if (options.SaveXBMCMeta)
                    SaveImage(directory, "fanart.jpg", null, 0, null, Providers.TVShowPosterType.Backdrop, level);
                if (options.SaveMyMoviesMeta)
                    SaveImage(directory, "backdrop.jpg", null, 0, null, Providers.TVShowPosterType.Backdrop, level);
            }

            if (options.DownloadAllBackdrops)
            {
                Posters[] backdrops = tvdb.GetPosters(series.id, MediaScout.Providers.TVShowPosterType.Backdrop, null);
                
                Message("Download All Backdrops", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in backdrops)
                {
                    if(options.SaveXBMCMeta)
                        SaveImage(directory, "fanart" + i + ".jpg", backdrops, i, null, Providers.TVShowPosterType.Backdrop, level + 1);
                    if(options.SaveMyMoviesMeta)
                        SaveImage(directory, "backdrop" + i + ".jpg", backdrops, i, null, Providers.TVShowPosterType.Backdrop, level + 1);
                    i++;
                }
            }

            #endregion

            #region Save Series Banner

            if (options.GetSeriesPosters)
                SaveImage(directory, "banner.jpg", null, 0, null, Providers.TVShowPosterType.Banner, level);

            if (options.DownloadAllBanners)
            {
                Posters[] banners = tvdb.GetPosters(series.id, MediaScout.Providers.TVShowPosterType.Banner, null);

                Message("Downloading All Banners ", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in banners)
                {
                    SaveImage(directory, "banner" + i + ".jpg", banners, i, null, Providers.TVShowPosterType.Banner, level+1);
                    i++;
                }
            }

            #endregion

            #endregion

            #region Save Actors Thumb
            if (options.SaveActors)
            {
                if (options.SaveXBMCMeta)
                {
                    Message("Saving Actors Thumb in " + new Person().GetXBMCDirectory() + "\\", MediaScoutMessage.MessageType.Task, level);
                    if(series.Persons.Count != 0)
                    {
                        String ActorsDir = directory + "\\" + new Person().GetXBMCDirectory();
                        if (!Directory.Exists(ActorsDir))
                            mdf.CreateHiddenFolder(ActorsDir);

                        foreach (Person p in series.Persons)
                        {
                            if (p.Type == "Actor")
                            {
                                if (!String.IsNullOrEmpty(p.Thumb))
                                {
                                    String Filename = p.GetXBMCFilename();
                                    String Filepath = ActorsDir + "\\" + Filename;
                                    if (!File.Exists(Filepath) || options.ForceUpdate)
                                        p.SaveThumb(Filepath);
                                }
                            }
                        }
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                    else
                        Message("No Actors Found", MediaScoutMessage.MessageType.TaskError, level);
                }

                if (options.SaveMyMoviesMeta)
                {
                    Message("Saving Actors Thumb in ImagesByName\\", MediaScoutMessage.MessageType.Task, level);
                    if (Directory.Exists(ImagesByNameLocation))
                    {
                        if (series.Persons.Count != 0)
                        {
                            foreach (Person p in series.Persons)
                            {
                                if (p.Type == "Actor")
                                {
                                    if (!String.IsNullOrEmpty(p.Thumb))
                                    {                                       
                                        String ActorsDir = ImagesByNameLocation + "\\" + p.GetMyMoviesDirectory();
                                        String Filepath = ActorsDir + "\\" + p.GetMyMoviesFilename();
                                        if (!File.Exists(Filepath) || options.ForceUpdate)
                                        {
                                            if (!Directory.Exists(ActorsDir))
                                                Directory.CreateDirectory(ActorsDir);

                                            p.SaveThumb(Filepath);
                                        }
                                    }
                                }
                            }
                            Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                        }
                        else
                            Message("No Actors Found", MediaScoutMessage.MessageType.TaskError, level);
                    }
                    else
                        Message("ImagesByName Location Not Defined", MediaScoutMessage.MessageType.TaskError, level);
                }

            }
            #endregion

            #region Identify and Move files that are in the root/series folder but should be in a Season folder

            if (options.MoveFiles == true)
            {
                Message("Sorting loose episodes", MediaScoutMessage.MessageType.Task, level);

                DirectoryInfo diRoot = new DirectoryInfo(directory);
                List<String> filetypes = new List<String>(options.AllowedFileTypes);
                foreach (FileInfo fiRoot in diRoot.GetFiles())
                {
                    if (filetypes.Contains(fiRoot.Extension.ToLower()))
                        MoveFileToAppropriateFolder(directory, fiRoot, -1, -1, level+1);
                }
            }

            #endregion

            #region Process the season folders

            DirectoryInfo diShow = new DirectoryInfo(directory);            
            foreach (DirectoryInfo diSeason in diShow.GetDirectories())
                ProcessSeasonDirectory(directory, diSeason, level + 1);
            
            #endregion

            return name;
        }
        
        #endregion
        
        #region Process Season

        public String ProcessSeasonDirectory(String ShowDirectory, DirectoryInfo diSeason, int level)
        {
            String name = diSeason.Name;

            if (level == -1)
                level = this.level;

            Regex rSeasons = new Regex(options.SeasonFolderName + ".{0,1}([0-9]+)", RegexOptions.IgnoreCase);
            MatchCollection mc = rSeasons.Matches(diSeason.Name);
            if (mc.Count > 0 || diSeason.Name == options.SpecialsFolderName )
            {
                Message("Processing " + diSeason.Name, MediaScoutMessage.MessageType.Task, level);

                int seasonNum = 0;
                if(diSeason.Name != options.SpecialsFolderName)
                    //Using int to make "01" becoming "1" later
                    seasonNum = Int32.Parse(mc[0].Groups[1].Captures[0].Value);

                //FreQi - Make sure the discovered season number is valid (in the metadata from theTVDB.com)
                if (series.Seasons.ContainsKey(seasonNum))
                {
                    if (diSeason.Name != options.SpecialsFolderName)
                    {
                        String newName = options.SeasonFolderName + " " + seasonNum.ToString().PadLeft(options.SeasonNumZeroPadding, '0');
                        if (options.RenameFiles)
                        {
                            String newPath = ShowDirectory + "\\" + newName;
                            if (diSeason.Name != newName)
                            {
                                if (mdf.MergeDirectories(diSeason.FullName, newPath, options.overwrite))
                                    name = "d:" + newName;
                                else
                                    name = newName;
                            }
                        }
                        ProcessSeason(ShowDirectory, newName, seasonNum, level + 1);
                    }
                    else
                        ProcessSeason(ShowDirectory, diSeason.Name, seasonNum, level + 1);                    
                }
                else
                    Message("Invalid Season folder:" + diSeason.Name, MediaScoutMessage.MessageType.TaskError, level);
            }
            //else if ((diSeason.Name != "metadata") && ((diSeason.Name != ".actors")))
              //  Message("Folder not Identified", MediaScoutMessage.MessageType.TaskError, level);
            return name;
        }
        public void ProcessSeason(String directory, String seasonFldr, int seasonNum, int level)
        {
            Message("Valid", MediaScoutMessage.MessageType.TaskResult, level);

            #region Download Season Poster

            //Save the season poster, if there is one and we're supposed to
            if (options.GetSeasonPosters)
                SaveImage(directory + "\\" + seasonFldr, "folder.jpg", null, 0, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Poster, level);
            
            if (options.DownloadAllSeasonPosters)
            {
                Posters[] seasonPosters = tvdb.GetPosters(series.id, MediaScout.Providers.TVShowPosterType.Season_Poster, seasonNum.ToString());

                Message("Downloading All " + options.SeasonFolderName + " Poster", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in seasonPosters)
                {
                    SaveImage(directory + "\\" + seasonFldr, "folder" + i + ".jpg", seasonPosters, i, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Poster, level + 1);
                    i++;
                }
            }
            #endregion

            #region Download Season Backdrop
           
            if (options.GetSeasonPosters)
            {
                if (options.SaveXBMCMeta)
                    SaveImage(directory + "\\" + seasonFldr, "fanart.jpg", null, 0, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Backdrop, level);
                if (options.SaveMyMoviesMeta)
                    SaveImage(directory + "\\" + seasonFldr, "backdrop.jpg", null, 0, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Backdrop, level);
            }

            if (options.DownloadAllSeasonBackdrops)
            {
                Posters[] seasonBackdrops = tvdb.GetPosters(series.id, MediaScout.Providers.TVShowPosterType.Season_Backdrop, seasonNum.ToString());
                Message("Downloading All " + options.SeasonFolderName + " Backdrops", MediaScoutMessage.MessageType.Task, level + 1);
                int i = 0;
                foreach (Posters p in seasonBackdrops)
                {
                    if (options.SaveXBMCMeta)
                        SaveImage(directory + "\\" + seasonFldr, "fanart" + i + ".jpg", seasonBackdrops, i, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Backdrop, level + 1);
                    if (options.SaveMyMoviesMeta)
                        SaveImage(directory + "\\" + seasonFldr, "backdrop" + i + ".jpg", seasonBackdrops, i, seasonNum.ToString(), MediaScout.Providers.TVShowPosterType.Season_Backdrop, level + 1);
                    i++;
                }
            }
            #endregion

            #region Process Files

            List<String> filetypes = new List<String>(options.AllowedFileTypes);
            DirectoryInfo diEpisodes = new DirectoryInfo(String.Format("{0}\\{1}", directory, seasonFldr));

            foreach (FileInfo fi in diEpisodes.GetFiles())
                if (filetypes.Contains(fi.Extension.ToLower()))
                    ProcessEpisode(directory, fi, seasonNum, true, level + 1);

            #endregion
        }

        #endregion

        #region Process Episode Routines
        
        public class EpisodeInfo
        {
            public int SeasonID = -1;
            public int EpisodeID = -1;
        }
        public EpisodeInfo GetSeasonAndEpisodeIDFromFile(String FileName)
        {
            EpisodeInfo ei = new EpisodeInfo();
            //FreQi - We've got a video file, let's see if we can determine what episode it is

            //Let's look for a couple patterns...
            //Is "S##E##" or "#x##" somewhere in there ?
            Match m = Regex.Match(FileName, "S(?<se>[0-9]{1,2})E(?<ep>[0-9]{1,3})|(?<se>[0-9]{1,2})x(?<ep>[0-9]{1,3})", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Is "S##?EP##" or S#?EP##?
            m = Regex.Match(FileName, "S(?<se>[0-9]{1,2}).EP(?<ep>[0-9]{1,3})", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Does the file START WITH just "###" (SEE) or #### (SSEE) ? (if not found yet)
            m = Regex.Match(FileName, "^(?<se>[0-9]{1,2})(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success)
            {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            //Is it just the two digit episode number maybe?
            m = Regex.Match(FileName, "^(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success)
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);

            //Does the file NOT START WITH just "###" (SEE) or #### (SSEE) ? (if not found yet)
            m = Regex.Match(FileName, "(?<se>[0-9]{1,2})(?<ep>[0-9]{2})", RegexOptions.IgnoreCase);
            if (ei.EpisodeID == -1 && m.Success)
            {
                ei.EpisodeID = Int32.Parse(m.Groups["ep"].Value);
                ei.SeasonID = Int32.Parse(m.Groups["se"].Value);
            }

            return ei;
        }
        public String ProcessEpisode(String ShowDirectory, FileInfo fi, int seasonNum, bool IsInsideShowDir, int level)
        {
            if (level == -1)
                level = this.level;

            #region Move File to its TVShow Directory
            
            if (options.MoveFiles)
            {
                if (!IsInsideShowDir)
                {
                    String newDirPath = ShowDirectory + "\\" + series.SeriesName;
                    if (!Directory.Exists(newDirPath))
                        Directory.CreateDirectory(newDirPath);
                    ShowDirectory = newDirPath;
                    
                    FileInfo OldFile = new FileInfo(fi.FullName);
                    String newFilePath = ShowDirectory + "\\" + fi.Name;

                    fi = MoveFile(fi.FullName, newFilePath, level, false);

                    if (fi.FullName == newFilePath)
                        MoveRelatedFiles(OldFile, ShowDirectory, level);                        
                }
            }

            #endregion

            Message("Processing File : " + fi.Name, MediaScoutMessage.MessageType.Task, level);

            try
            {
                EpisodeInfo ei = GetSeasonAndEpisodeIDFromFile(fi.Name);
                
                String SeasonID = (ei.SeasonID != -1) ? ei.SeasonID.ToString() : "Unable to extract";
                String EpisodeID = (ei.EpisodeID != -1) ? ei.EpisodeID.ToString() : "Unable to extract";

                Message("Season : " + SeasonID + ", Episode : " + EpisodeID, (ei.SeasonID != -1 && ei.EpisodeID != -1) ? MediaScoutMessage.MessageType.TaskResult : MediaScoutMessage.MessageType.TaskError, level);

                //So, did we find an episode number?
                if (ei.SeasonID != -1 && ei.EpisodeID != -1)
                {
                    //Do we know what season this file "thinks" it's belongs to and is it in the right folder?
                    if (ei.SeasonID != seasonNum)
                        if (options.MoveFiles)
                            fi = MoveFileToAppropriateFolder(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID, level + 1);

                    if (series.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                        return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID, level + 1);
                    else
                    {
                        Message(String.Format("Episode {0} Not Found In Season {1}", ei.EpisodeID, ei.SeasonID), MediaScoutMessage.MessageType.Error, level + 1);
                        if (series.LoadedFromCache)
                        {
                            //Information in cache may be old, refetch and check again
                            Message("Updating Cache", MediaScoutMessage.MessageType.Task, level + 1);
                            series = tvdb.GetTVShow(series.SeriesID, DateTime.Now, level + 2);
                            if (series != null)
                            {
                                if (series.Seasons[ei.SeasonID].Episodes.ContainsKey(ei.EpisodeID))
                                    return ProcessFile(ShowDirectory, fi, ei.SeasonID, ei.EpisodeID, level + 1);
                                else
                                    Message("Invalid Episode, Skipping", MediaScoutMessage.MessageType.Error, level + 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message, MediaScoutMessage.MessageType.Error, level);
            }
            return null;
        }
        public String ProcessFile(String ShowDirectory, FileInfo file, int seasonID, int episodeID, int level)
        {            
            String name = file.Name;            
            EpisodeXML episode = series.Seasons[seasonID].Episodes[episodeID];

            String metadataFolder = null;
            if (options.SaveMyMoviesMeta)
            {
                metadataFolder = episode.GetMetadataFolder(file.DirectoryName);
                if (!Directory.Exists(metadataFolder))
                    mdf.CreateHiddenFolder(metadataFolder);
            }

            #region Rename Files
            if (options.RenameFiles)
            {
                //Calculate the renamed file
                String newName = String.Format(options.RenameFormat, series.SeriesName, seasonID.ToString().PadLeft(options.SeasonNumZeroPadding, '0'), episode.EpisodeName.Trim(), episode.ID.ToString().PadLeft(options.EpisodeNumZeroPadding, '0')).Replace("?", "").Replace(":", "");
                newName = Regex.Replace(newName, @"[\<\(\.\|\n\)\*\?\\\/\>\""]", "-");
                String newPath = file.DirectoryName + "\\" + newName + file.Extension;
                
                //Rename file
                if (file.Name != newName + file.Extension)
                {
                    FileInfo OldFile = new FileInfo(file.FullName);
                    file = MoveFile(file.FullName, newPath, level, true);
                    if (file.Name == newName + file.Extension)
                    {
                        RenameRelatedFiles(OldFile, newName, level);                        
                        name = newName + file.Extension;
                    }
                }
            }
            #endregion

            #region Save Episode Metadata

            if (options.SaveXBMCMeta)
            {
                Message("Saving Metadata as " + episode.GetNFOFileName(file.Name.Replace(file.Extension, "")), MediaScoutMessage.MessageType.Task, level);
                if (options.ForceUpdate == true || !File.Exists(episode.GetNFOFile(file.DirectoryName, file.Name.Replace(file.Extension, ""))))
                {
                    episode.SaveNFO(file.DirectoryName, file.Name.Replace(file.Extension, ""));
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                }
                else
                    Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
            }

            if (options.SaveMyMoviesMeta)
            {
                Message("Saving Metadata as " + episode.GetXMLFilename(file.Name.Replace(file.Extension, "")), MediaScoutMessage.MessageType.Task, level);
                if (options.ForceUpdate == true || !File.Exists(episode.GetXMLFile(file.DirectoryName, file.Name.Replace(file.Extension, ""))))
                {
                    episode.SaveXML(file.DirectoryName, file.Name.Replace(file.Extension, ""));
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                }
                else
                    Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);

            }

            #endregion

            #region Get the poster
            if (options.GetEpisodePosters)
            {                
                if (!String.IsNullOrEmpty(episode.PosterUrl))
                {
                    Posters p = new Posters();
                    p.Poster = episode.PosterUrl;
                    try
                    {
                        if (options.SaveXBMCMeta)
                        {
                            Message("Saving Episode Poster as " + episode.GetXBMCThumbFilename(file.Name.Replace(file.Extension, "")), MediaScoutMessage.MessageType.Task, level);
                            String filename = episode.GetXBMCThumbFile(file.DirectoryName, file.Name.Replace(file.Extension, ""));
                            if (!File.Exists(filename))
                            {
                                p.SavePoster(filename);
                                Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                            }
                            else
                                Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                        }
                        if (options.SaveMyMoviesMeta)
                        {
                            Message("Saving Episode Poster as " + episode.GetMyMoviesThumbFilename(), MediaScoutMessage.MessageType.Task, level);
                            String filename = episode.GetMyMoviesThumbFile(metadataFolder);
                            if (!File.Exists(filename))
                            {
                                p.SavePoster(filename);
                                Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                            }
                            else
                                Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                        }
                    }
                    catch (Exception ex)
                    {
                        Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                    }
                }
                else
                {
                    if (options.SaveXBMCMeta)
                    {
                        Message("Saving thumbnail from video as " + episode.GetXBMCThumbFilename(file.Name.Replace(file.Extension, "")), MediaScoutMessage.MessageType.Task, level);
                        String filename = episode.GetXBMCThumbFile(file.DirectoryName, file.Name.Replace(file.Extension, ""));
                        if (!File.Exists(filename))
                        {
                            //VideoInfo.SaveThumb(file.FullName, filename, 0.25);
                            Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                        }
                        else
                            Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                    
                    if (options.SaveMyMoviesMeta)
                    {
                        Message("Saving thumbnail from video as " + episode.GetMyMoviesThumbFilename(), MediaScoutMessage.MessageType.Task, level);
                        String Filename = episode.GetMyMoviesThumbFile(metadataFolder);
                        if(!File.Exists(Filename))
                        {
                            //VideoInfo.SaveThumb(file.FullName, Filename, 0.25);
                            episode.PosterName = episode.EpisodeID + ".jpg";
                            Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                        }
                        else
                            Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                }
            }
            #endregion           
            
            return name;
        }
        public FileInfo MoveFileToAppropriateFolder(String ShowDirectory, FileInfo fi, int seasonID, int episodeID, int level)
        {
            EpisodeInfo ei = null;
         
            if (seasonID == -1 || episodeID == -1)
                ei = GetSeasonAndEpisodeIDFromFile(fi.Name);

            if(seasonID == -1)
                seasonID = ei.SeasonID;
            if(episodeID == -1)
                episodeID = ei.EpisodeID;

            Message("File " + fi.Name, MediaScoutMessage.MessageType.Task, level);
            
            String SeasonID = (seasonID != -1) ? seasonID.ToString() : "Unable to extract";
            String EpisodeID = (episodeID != -1) ? episodeID.ToString() : "Unable to extract";

            Message("Season : " + SeasonID + ", Episode : " + EpisodeID, (seasonID != -1 && episodeID != -1) ? MediaScoutMessage.MessageType.TaskResult : MediaScoutMessage.MessageType.TaskError, level);
            
            if (seasonID != -1 && episodeID != -1)
            {
                //FreQi - Make sure the discovered season number is valid (in the metadata from theTVDB.com)
                if (series.Seasons.ContainsKey(seasonID))
                {
                    if (series.Seasons[seasonID].Episodes.ContainsKey(episodeID))
                    {
                        //the season is valid, do we already have a folder for it?

                        ////I know this is sloppy, but loop through all the known seasons, then compare the numerical
                        //// values (not strings) to see if there's a match.  If we find one, set the folder name to
                        //// the one that exists so we don't make a "Season 01" when "Season 1" exists.
                        //foreach (String knownSeason in seasons)
                        //{
                        //    if (Int32.Parse(knownSeason) == Int32.Parse(seasonID))
                        //        seasonFolder = seasonFldrs[seasons.IndexOf(knownSeason)];
                        //}

                        String seasonFolderName = (seasonID != 0) ? options.SeasonFolderName + " " + seasonID.ToString().PadLeft(options.SeasonNumZeroPadding, '0') : options.SpecialsFolderName;
                        String seasonFolderPath = ShowDirectory + "\\" + seasonFolderName;

                        //Create the season folder if we have to
                        if (!Directory.Exists(seasonFolderPath))
                            Directory.CreateDirectory(seasonFolderPath);

                        String newPath = seasonFolderPath + "\\" + fi.Name;

                        if (fi.FullName != newPath)
                        {
                            FileInfo OldFile = new FileInfo(fi.FullName);

                            //Finally, move the file to it's new place.
                            fi = MoveFile(fi.FullName, newPath, level, false);
                            if (fi.FullName == newPath)
                                MoveRelatedFiles(OldFile, seasonFolderPath, level + 1);
                        }
                    }
                    else
                        Message(String.Format("Season {0} Not Found In {1}, Skipping", seasonID, series.Title), MediaScoutMessage.MessageType.Error, level);
                }
                else 
                    Message(String.Format("Season {0} Not Found In {1}, Skipping", seasonID, series.Title), MediaScoutMessage.MessageType.Error, level);
            }            
            return fi;
        }

        #endregion       

        #region Move and Rename File Functions

        #region Move and Rename Related File Functions
        private void RenameRelatedFiles(FileInfo fi, String NewName, int level)
        {
            RenameSubtitle(fi, NewName, level);
            RenameXBMCMeta(fi, NewName, level);
            RenameMyMoviesMeta(fi, NewName, level);
        }
        private void MoveRelatedFiles(FileInfo fi, String NewPath, int level)
        {
            MoveSubtitle(fi, NewPath, level);
            MoveXBMCMeta(fi, NewPath, level); ;
            MoveMyMoviesMeta(fi, NewPath, level);
        }
        #endregion

        #region Move and Rename Subtitle Functions
        private void RenameSubtitle(FileInfo fi, String NewName, int level)
        {
            foreach (String subtitleExt in options.AllowedSubtitles)
            {
                String src = fi.FullName.Replace(fi.Extension, subtitleExt);
                if (File.Exists(src))
                {
                    String dest = fi.DirectoryName + "\\" + NewName + subtitleExt;
                    MoveFile(src, dest, level, true);
                }
            }
        }
        private void MoveSubtitle(FileInfo fi, String NewPath, int level)
        {
            foreach (String subtitleExt in options.AllowedSubtitles)
            {
                String src = fi.FullName.Replace(fi.Extension, subtitleExt);
                if (File.Exists(src))
                {
                    String dest = NewPath + "\\" + fi.Name + subtitleExt;
                    MoveFile(src, dest, level, false);
                }
            }
        }
        #endregion

        #region Move and Rename XBMC Meta Functions
        private void RenameXBMCMeta(FileInfo fi, String NewName, int level)
        {
            EpisodeXML e = new EpisodeXML();
            String src = e.GetNFOFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetNFOFile(fi.DirectoryName, NewName);
                MoveFile(src, dest, level, true);
            }
            src = e.GetXBMCThumbFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetXBMCThumbFile(fi.DirectoryName, NewName);
                MoveFile(src, dest, level, true);
            }
        }
        private void MoveXBMCMeta(FileInfo fi, String NewPath, int level)
        {
            EpisodeXML e = new EpisodeXML();
            String src = e.GetNFOFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetNFOFile(NewPath, fi.Name.Replace(fi.Extension, ""));
                MoveFile(src, dest, level, false);
            }
            src = e.GetXBMCThumbFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetXBMCThumbFile(NewPath, fi.Name.Replace(fi.Extension, ""));
                MoveFile(src, dest, level, false);
            }
        }
        #endregion
        
        #region Move and Rename MyMovies Meta Functions
        private void RenameMyMoviesMeta(FileInfo fi, String NewName, int level)
        {
            EpisodeXML e = new EpisodeXML();
            String src = e.GetXMLFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetXMLFile(fi.DirectoryName, NewName);
                MoveFile(src, dest, level, true);
            }
            src = e.GetMyMoviesThumbFile(fi.DirectoryName);
            if (File.Exists(src))
            {
                String dest = e.GetMyMoviesThumbFile(fi.DirectoryName);
                MoveFile(src, dest, level, true);
            }

        }
        private void MoveMyMoviesMeta(FileInfo fi, String NewPath, int level)
        {
            EpisodeXML e = new EpisodeXML();
            String src = e.GetXMLFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src))
            {
                String dest = e.GetXMLFile(NewPath, fi.Name.Replace(fi.Extension, ""));
                MoveFile(src, dest, level, false);
            }
            src = e.GetMyMoviesThumbFile(fi.DirectoryName);
            if (File.Exists(src))
            {
                String dest = e.GetMyMoviesThumbFile(NewPath);
                MoveFile(src, dest, level, false);
            }
        }
        #endregion

        #region Move and Rename File Function
        private FileInfo MoveFile(String src, String dest, int level, bool IsRenameOperation)
        {
            FileInfo fi = new FileInfo(src);
            bool error = false;
            try
            {
                if (IsRenameOperation)
                    Message("Renaming " + fi.Name + " to", MediaScoutMessage.MessageType.Task, level);
                else
                    Message("Moving " + fi.FullName + " to", MediaScoutMessage.MessageType.Task, level);

                fi = mff.MoveFile(fi, dest, options.overwrite);
            }
            catch (Exception ex)
            {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                error = true;
            }

            if (fi.FullName == dest)
            {
                if (IsRenameOperation)
                    Message(fi.Name, MediaScoutMessage.MessageType.TaskResult, level);
                else
                    Message(fi.DirectoryName + "\\", MediaScoutMessage.MessageType.TaskResult, level);
            }
            else if(!error)
                Message("Canceled", MediaScoutMessage.MessageType.TaskError, level);

            return fi;
        }
        #endregion
        
        #endregion

        #region Save Functions

        private void SaveMeta(String directory, int level)
        {
            try
            {
                //Save Movie NFO
                if (options.SaveXBMCMeta)
                {
                    Message("Saving Metadata as " + series.GetNFOFile(directory), MediaScoutMessage.MessageType.Task, level);
                    if (options.ForceUpdate == true || !File.Exists(series.GetNFOFile(directory)))
                    {
                        series.SaveNFO(directory);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                    else
                        Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                }

                //Save Movie XML
                if (options.SaveMyMoviesMeta)
                {
                    Message("Saving Metadata as " + series.GetXMLFile(directory), MediaScoutMessage.MessageType.Task, level);
                    if (options.ForceUpdate == true || !File.Exists(series.GetXMLFile(directory)))
                    {
                        series.SaveXML(directory);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                    else
                        Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }
        }
        private void SaveImage(String directory, String filename, Posters[] images, int index, String SeasonNum, Providers.TVShowPosterType ptype, int level)
        {
            Message("Saving " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + " as " + filename, MediaScoutMessage.MessageType.Task, level);
            if (!File.Exists(directory + "\\" + filename) || options.ForceUpdate == true)
            {
                try
                {
                    if(images == null)
                        images = tvdb.GetPosters(series.ID, ptype, SeasonNum);
                    if (images != null)
                    {
                        //Message("Saving " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + " (" + images[index].Poster + ") as " + filename, MediaScoutMessage.MessageType.Task, level);
                        images[index].SavePoster(directory + "\\" + filename);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    }
                    else
                        Message("No " + ptype.ToString().Replace("_", (SeasonNum != null) ? " " + SeasonNum + " " : " ") + "s Found", MediaScoutMessage.MessageType.TaskError, level);
                }
                catch (Exception ex)
                {
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                }
            }
            else
                Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
        }

        #endregion
    }
}
