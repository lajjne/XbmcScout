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

namespace XbmcScout {
    public class MovieScout {
        MediaScoutMessage.Message Message;
        MovieScoutOptions options;
        Providers.TheMovieDBProvider tmdb;
        //DirFunc mdf = new DirFunc();
        //MoveFileFunc mff = new MoveFileFunc();

        public MovieXML m;

        int level = 1;

        public MovieScout(MovieScoutOptions options, MediaScoutMessage.Message Message) {
            this.options = options;
            this.Message = Message;
            tmdb = new XbmcScout.Providers.TheMovieDBProvider(Message);
        }

        public String ProcessDirectory(String directory) {
            String name = m.Title;
            int level = this.level;

            SaveMeta(directory, level);

            if (options.GetPoster) {
                SaveImage(directory, "folder.jpg", null, 0, Providers.MoviePosterType.Poster, level);
            }         

            if (options.GetBackdrop) {
                SaveImage(directory, "fanart.jpg", null, 0, MoviePosterType.Backdrop, level);
            }

            return name;
        }

        private void SaveMeta(String directory, int level) {
            try {
                //Save Movie NFO
                Message("Saving Metadata as " + m.GetNFOFile(directory), MediaScoutMessage.MessageType.Task, level);
                if (options.Overwrite == true || !File.Exists(m.GetNFOFile(directory))) {
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

            if (!File.Exists(directory + "\\" + filename) || options.Overwrite == true) {
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