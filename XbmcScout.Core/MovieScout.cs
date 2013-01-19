using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Net;

using XbmcScout.Core.Providers;

namespace XbmcScout.Core {
    public class MovieScout {
        MediaScoutMessage.Message Message;
        MovieScoutOptions options;
        Providers.TheMovieDBProvider tmdb;
        String ImagesByNameLocation = null;
        DirFunc mdf = new DirFunc();
        MoveFileFunc mff = new MoveFileFunc();

        public MovieXML m;

        int level = 1;

        public MovieScout(MovieScoutOptions options, MediaScoutMessage.Message Message, String ImagesByNameLocation) {
            this.options = options;
            this.Message = Message;
            this.ImagesByNameLocation = ImagesByNameLocation;
            tmdb = new XbmcScout.Core.Providers.TheMovieDBProvider(Message);
        }

        #region Proces Movie Directory

        private String GetMovieDirName() {
            String NewDirName = String.Format(options.DirRenameFormat, m.Title, m.ProductionYear).Replace("?", "").Replace(":", "");
            return Regex.Replace(NewDirName, @"[\<\.\|\n\*\?\\\/\>]", string.Empty);
        }
        public String ProcessDirectory(String directory) {
            String name = m.Title;
            int level = this.level;

            #region Rename Dir

            String NewDirPath = directory;
            if (options.RenameFiles) {
                name = mdf.GetDirectoryName(directory);
                String NewDirName = GetMovieDirName();
                NewDirPath = directory.Replace(name, "") + NewDirName;
                if (name != NewDirName) {
                    if (mdf.MergeDirectories(directory, NewDirPath, options.overwrite))
                        name = "d:" + NewDirName;
                    else
                        name = NewDirName;
                    directory = NewDirPath;
                }
            }
            #endregion

            SaveMeta(directory, level);

            #region Proces Movie Images

            #region Save Movie Poster

            if (options.GetMoviePosters)
                SaveImage(directory, "folder.jpg", null, 0, Providers.MoviePosterType.Poster, level);

            if (options.DownloadAllPosters) {
                Posters[] posters = tmdb.GetPosters(m.ID, XbmcScout.Core.Providers.MoviePosterType.Poster);

                Message("Downloading All Posters", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in posters) {
                    SaveImage(directory, "folder" + i + ".jpg", posters, i, Providers.MoviePosterType.Poster, level + 1);
                    i++;
                }
            }

            #endregion

            #region Save Movie Backdrop

            if (options.GetMoviePosters) {
                if (options.SaveXBMCMeta)
                    SaveImage(directory, "fanart.jpg", null, 0, MoviePosterType.Backdrop, level);
                if (options.SaveMyMoviesMeta)
                    SaveImage(directory, "backdrop.jpg", null, 0, MoviePosterType.Backdrop, level);
            }

            if (options.DownloadAllBackdrops) {
                Posters[] backdrops = tmdb.GetPosters(m.ID, MoviePosterType.Backdrop);
                Message("Downloading All Backdrops", MediaScoutMessage.MessageType.Task, level);
                int i = 0;
                foreach (Posters p in backdrops) {
                    if (options.SaveXBMCMeta)
                        SaveImage(directory, "fanart" + i + ".jpg", backdrops, i, MoviePosterType.Backdrop, level + 1);
                    if (options.SaveMyMoviesMeta)
                        SaveImage(directory, "backdrop" + i + ".jpg", backdrops, i, MoviePosterType.Backdrop, level + 1);
                    i++;
                }
            }

            #endregion

            #endregion

            #region Process Files in Movie Directory

            DirectoryInfo di = new DirectoryInfo(NewDirPath);
            List<String> filetypes = new List<String>(options.AllowedFileTypes);
            foreach (FileInfo fiRoot in di.GetFiles()) {
                if (filetypes.Contains(fiRoot.Extension.ToLower()))
                    ProcessFile(directory, fiRoot, true, level);
            }

            #endregion

            #region Save Actors Thumb
            if (options.SaveActors) {
                if (options.SaveXBMCMeta) {
                    Message("Saving Actors Thumb in " + new Person().GetXBMCDirectory() + "\\", MediaScoutMessage.MessageType.Task, level);
                    if (m.Persons.Count != 0) {
                        String ActorsDir = directory + "\\" + new Person().GetXBMCDirectory();
                        if (!Directory.Exists(ActorsDir))
                            mdf.CreateHiddenFolder(ActorsDir);

                        foreach (Person p in m.Persons) {
                            if (p.Type == "Actor") {
                                if (!String.IsNullOrEmpty(p.Thumb)) {
                                    String Filename = p.GetXBMCFilename();
                                    String Filepath = ActorsDir + "\\" + Filename;
                                    if (!File.Exists(Filepath) || options.ForceUpdate)
                                        p.SaveThumb(Filepath);
                                }
                            }
                        }
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    } else
                        Message("No Actors Found", MediaScoutMessage.MessageType.TaskError, level);
                }

                if (options.SaveMyMoviesMeta) {
                    Message("Saving Actors Thumb in ImagesByName\\", MediaScoutMessage.MessageType.Task, level);
                    if (!String.IsNullOrEmpty(ImagesByNameLocation)) {
                        if (m.Persons.Count != 0) {
                            foreach (Person p in m.Persons) {
                                if (p.Type == "Actor") {
                                    if (!String.IsNullOrEmpty(p.Thumb)) {
                                        String ActorsDir = ImagesByNameLocation + "\\" + p.GetMyMoviesDirectory();
                                        String Filepath = ActorsDir + "\\" + p.GetMyMoviesFilename();
                                        if (!File.Exists(Filepath) || options.ForceUpdate) {
                                            if (!Directory.Exists(ActorsDir))
                                                Directory.CreateDirectory(ActorsDir);

                                            p.SaveThumb(Filepath);
                                        }
                                    }
                                }
                            }
                            Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                        } else
                            Message("No Actors Found", MediaScoutMessage.MessageType.TaskError, level);
                    } else
                        Message("ImagesByName Location Not Defined", MediaScoutMessage.MessageType.TaskError, level);
                }

            }
            #endregion

            return name;
        }

        #endregion

        #region Process Movie File

        private MovieType GetMovieFileType(FileInfo fi) {
            MovieType type = MovieType.None;

            //Identify the type of file
            Match regex;

            //Check if its a Tailer
            regex = Regex.Match(fi.Name, ".trailer", RegexOptions.IgnoreCase);
            if (regex.Success)
                type = MovieType.Trailer;

            regex = Regex.Match(fi.Name, ".sample", RegexOptions.IgnoreCase);
            if (type == MovieType.None && regex.Success)
                type = MovieType.Sample;

            regex = Regex.Match(fi.Name, ".cd([0-9]+)", RegexOptions.IgnoreCase);
            if (type == MovieType.None && regex.Success)
                type = MovieType.Multi_File_Rip;

            if (type == MovieType.None)
                type = MovieType.Single_File_Rip;

            return type;
        }
        public String ProcessFile(String MovieDirectory, FileInfo fi, bool IsInsideMovieFolder, int level) {
            if (level == -1)
                level = this.level;

            #region Move File to its Movie Directory

            if (options.MoveFiles) {
                if (!IsInsideMovieFolder) {
                    String newDirPath = MovieDirectory + "\\" + GetMovieDirName();
                    if (!Directory.Exists(newDirPath))
                        Directory.CreateDirectory(newDirPath);
                    MovieDirectory = newDirPath;

                    FileInfo OldFile = new FileInfo(fi.FullName);
                    String newFilePath = MovieDirectory + "\\" + fi.Name;

                    fi = MoveFile(fi.FullName, newFilePath, level, false);
                    if (fi.FullName == newFilePath) {
                        MoveRelatedFiles(OldFile, MovieDirectory, level);
                        return ProcessDirectory(MovieDirectory);
                    }
                }
            }

            #endregion

            String name = fi.Name;

            Message("Processing File : " + fi.Name, MediaScoutMessage.MessageType.Task, level);

            MovieType type = GetMovieFileType(fi);
            Message(type.ToString().Replace("_", " "), MediaScoutMessage.MessageType.TaskResult, level);

            #region Rename Files
            if (options.RenameFiles) {
                if (type == MovieType.Single_File_Rip || type == MovieType.Multi_File_Rip || type == MovieType.Trailer || type == MovieType.Sample) {
                    //Calculate the renamed file
                    FileInfo OldFile = new FileInfo(fi.FullName);
                    String newName = String.Format(options.FileRenameFormat, m.Title, m.ProductionYear).Replace("?", "").Replace(":", "");
                    switch (type) {
                        case MovieType.Multi_File_Rip:
                            int DiscNo = -1;
                            Match regex;
                            if ((regex = Regex.Match(fi.Name, ".cd([0-9]+)", RegexOptions.IgnoreCase)).Success)
                                DiscNo = Int32.Parse(regex.Groups[1].Value);
                            newName += " - CD" + DiscNo;
                            break;
                        case MovieType.Trailer:
                            newName += " - Trailer";
                            break;
                        case MovieType.Sample:
                            newName += " - Sample";
                            break;
                    }
                    String newPath = fi.DirectoryName + "\\" + newName + fi.Extension;

                    if (fi.Name != newName + fi.Extension) {
                        if ((type == MovieType.Trailer || type == MovieType.Sample)
                            && (File.Exists(newPath) && !options.overwrite)) {
                            int i = 2;
                            bool success = false;
                            while (!success && i <= 10) {
                                newPath = fi.DirectoryName + "\\" + newName + " #" + i + fi.Extension;
                                if (!File.Exists(newPath))
                                    fi = MoveFile(fi.FullName, newPath, level, true);
                                else
                                    i++;
                                success = true;
                            }
                        } else
                            fi = MoveFile(fi.FullName, newPath, level, true);

                        if (fi.Name == newName + fi.Extension) {
                            RenameRelatedFiles(OldFile, newName, level + 1);
                            name = newName + fi.Extension;
                        }
                    }
                }
            }

            #endregion

            #region Save Movie File Images

            #region Save Movie File Poster
            if (options.GetMovieFilePosters) {
                if (options.SaveXBMCMeta)
                    SaveImage(fi.DirectoryName, m.GetXBMCThumbFilename(fi.Name.Replace(fi.Extension, "")), null, 0, MoviePosterType.File_Poster, level + 1);
            }
            #endregion

            #region Save Movie File Backdrop

            if (options.GetMovieFilePosters) {
                if (options.SaveXBMCMeta)
                    SaveImage(fi.DirectoryName, m.GetXBMCBackdropFilename(fi.Name.Replace(fi.Extension, "")), null, 0, MoviePosterType.File_Backdrop, level + 1);
            }

            #endregion

            #endregion

            name = fi.Name;
            return name;
        }

        #endregion

        #region Move and Rename File Functions

        #region Move and Rename Related File Functions
        private void RenameRelatedFiles(FileInfo fi, String NewName, int level) {
            RenameSubtitle(fi, NewName, level);
            RenameXBMCMeta(fi, NewName, level);
        }
        private void MoveRelatedFiles(FileInfo fi, String NewPath, int level) {
            MoveSubtitle(fi, NewPath, level);
            MoveXBMCMeta(fi, NewPath, level); ;
        }
        #endregion

        #region Move and Rename Subtitle Functions
        private void RenameSubtitle(FileInfo fi, String NewName, int level) {
            foreach (String subtitleExt in options.AllowedSubtitles) {
                String src = fi.FullName.Replace(fi.Extension, subtitleExt);
                if (File.Exists(src)) {
                    String dest = fi.DirectoryName + "\\" + NewName + subtitleExt;
                    MoveFile(src, dest, level, true);
                }
            }
        }
        private void MoveSubtitle(FileInfo fi, String NewPath, int level) {
            foreach (String subtitleExt in options.AllowedSubtitles) {
                String src = fi.FullName.Replace(fi.Extension, subtitleExt);
                if (File.Exists(src)) {
                    String dest = NewPath + "\\" + fi.Name + subtitleExt;
                    MoveFile(src, dest, level, false);
                }
            }
        }
        #endregion

        #region Move and Rename XBMC Meta Functions
        private void RenameXBMCMeta(FileInfo fi, String NewName, int level) {
            MovieXML m = new MovieXML();
            String src = m.GetXBMCThumbFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src)) {
                String dest = m.GetXBMCThumbFile(fi.DirectoryName, NewName);
                MoveFile(src, dest, level, true);
            }
            src = m.GetXBMCBackdropFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src)) {
                String dest = m.GetXBMCBackdropFile(fi.DirectoryName, NewName);
                MoveFile(src, dest, level, true);
            }
        }
        private void MoveXBMCMeta(FileInfo fi, String NewPath, int level) {
            MovieXML m = new MovieXML();
            String src = m.GetXBMCThumbFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src)) {
                String dest = m.GetXBMCThumbFile(NewPath, fi.Name.Replace(fi.Extension, ""));
                MoveFile(src, dest, level, false);
            }
            src = m.GetXBMCBackdropFile(fi.DirectoryName, fi.Name.Replace(fi.Extension, ""));
            if (File.Exists(src)) {
                String dest = m.GetXBMCBackdropFile(NewPath, fi.Name.Replace(fi.Extension, ""));
                MoveFile(src, dest, level, false);
            }
        }
        #endregion

        #region Move and Rename File Function
        private FileInfo MoveFile(String src, String dest, int level, bool IsRenameOperation) {
            FileInfo fi = new FileInfo(src);
            bool error = false;
            try {
                if (IsRenameOperation)
                    Message("Renaming " + fi.Name + " to", MediaScoutMessage.MessageType.Task, level);
                else
                    Message("Moving " + fi.FullName + " to", MediaScoutMessage.MessageType.Task, level);

                fi = mff.MoveFile(fi, dest, options.overwrite);
            } catch (Exception ex) {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                error = true;
            }

            if (fi.FullName == dest) {
                if (IsRenameOperation)
                    Message(fi.Name, MediaScoutMessage.MessageType.TaskResult, level);
                else
                    Message(fi.DirectoryName + "\\", MediaScoutMessage.MessageType.TaskResult, level);
            } else if (!error)
                Message("Canceled", MediaScoutMessage.MessageType.TaskError, level);

            return fi;
        }
        #endregion

        #endregion

        #region Save Functions

        private void SaveMeta(String directory, int level) {
            try {
                //Save Movie NFO
                if (options.SaveXBMCMeta) {
                    Message("Saving Metadata as " + m.GetNFOFile(directory), MediaScoutMessage.MessageType.Task, level);
                    if (options.ForceUpdate == true || !File.Exists(m.GetNFOFile(directory))) {
                        m.SaveNFO(directory);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    } else
                        Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                }

                //Save Movie XML
                if (options.SaveMyMoviesMeta) {
                    Message("Saving Metadata as " + m.GetXMLFile(directory), MediaScoutMessage.MessageType.Task, level);
                    if (options.ForceUpdate == true || !File.Exists(m.GetXMLFile(directory))) {
                        m.SaveXML(directory);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    } else
                        Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                }
            } catch (Exception ex) {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }
        }
        private void SaveImage(String directory, String filename, Posters[] images, int index, Providers.MoviePosterType ptype, int level) {
            Message("Saving " + ptype.ToString().Replace("_", " ") + " as " + filename, MediaScoutMessage.MessageType.Task, level);

            if (!File.Exists(directory + "\\" + filename) || options.ForceUpdate == true) {
                try {
                    if (images == null)
                        images = tmdb.GetPosters(m.ID, ptype);

                    if (images != null) {
                        //Message("Saving " + ptype.ToString().Replace("_", " ") + " (" + images[index].Poster + ") as " + filename, MediaScoutMessage.MessageType.Task, level);
                        images[index].SavePoster(directory + "\\" + filename);
                        Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                    } else
                        Message("No " + ptype.ToString().Replace("_", " ") + "s Found", MediaScoutMessage.MessageType.TaskError, level);
                } catch (Exception ex) {
                    Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
                }
            } else
                Message(ptype.ToString().Replace("_", " ") + " already exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
        }

        #endregion
    }

    public enum MovieType {
        None,
        DVD,
        Trailer,
        Multi_File_Rip,
        Single_File_Rip,
        Extras,
        Sample
    }
}