using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Net;

using XbmcScout.Providers;
using XbmcScout.Models;

namespace XbmcScout {
    public class MovieScout {
        MovieXML m;
        Flags flags;
        MediaScoutMessage.Message Message;
        TheMovieDBProvider tmdb;

        int level = 1;

        public MovieScout(MovieXML movie,Flags flags, MediaScoutMessage.Message Message) {
            this.m = movie;
            this.flags = flags;
            this.Message = Message;
            tmdb = new XbmcScout.Providers.TheMovieDBProvider(Message);
        }

        public String ProcessDirectory(String directory) {
            String name = m.Title;
            int level = this.level;

            SaveMeta(directory, level);

            if (flags.GetPosters) {
                SaveImage(directory, "folder.jpg", null, 0, Providers.MoviePosterType.Poster, level);
            }         

            if (flags.GetBackdrops) {
                SaveImage(directory, "fanart.jpg", null, 0, MoviePosterType.Backdrop, level);
            }

            return name;
        }

        private void SaveMeta(String directory, int level) {
            try {
                //Save Movie NFO
                Message("Saving Metadata as " + m.GetNFOFile(directory), MediaScoutMessage.MessageType.Task, level);
                if (flags.Overwrite == true || !File.Exists(m.GetNFOFile(directory))) {
                    m.SaveNFO(directory);
                    Message("Done", MediaScoutMessage.MessageType.TaskResult, level);
                } else {
                    Message("Already Exists, skipping", MediaScoutMessage.MessageType.TaskResult, level);
                }

            } catch (Exception ex) {
                Message(ex.Message, MediaScoutMessage.MessageType.TaskError, level);
            }
        }

        private void SaveImage(String directory, String filename, Posters[] images, int index, Providers.MoviePosterType ptype, int level) {
            Message("Saving " + ptype.ToString().Replace("_", " ") + " as " + filename, MediaScoutMessage.MessageType.Task, level);

            if (!File.Exists(directory + "\\" + filename) || flags.Overwrite == true) {
                try {
                    if (images == null)
                        images = tmdb.GetPosters(m.ID, ptype);

                    if (images != null) {
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
    }


}