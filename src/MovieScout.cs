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
        TheMovieDBProvider tmdb;
        MovieXML m;
        Options options;
        Log log;

        public MovieScout(MovieXML movie, Options options, Log log) {
            this.m = movie;
            this.options = options;
            this.log = log;
            tmdb = new TheMovieDBProvider(options.TMDbApiKey, log);
        }

        public String ProcessDirectory(String directory) {
            String name = m.Title;

            SaveMeta(directory);

            if (options.GetPosters) {
                SaveImage(directory, "folder.jpg", null, 0, Providers.MoviePosterType.Poster);
            }         

            if (options.GetBackdrops) {
                SaveImage(directory, "fanart.jpg", null, 0, MoviePosterType.Backdrop);
            }

            return name;
        }

        private void SaveMeta(String directory) {
            try {
                //Save Movie NFO
                log(Level.Debug, "Saving Metadata as " + m.GetNFOFile(directory));
                if (options.Overwrite == true || !File.Exists(m.GetNFOFile(directory))) {
                    m.SaveNFO(directory);
                } else {
                    log(Level.Info, "Already Exists, skipping");
                }

            } catch (Exception ex) {
                log(Level.Warn, ex.Message);
            }
        }

        private void SaveImage(String directory, String filename, Posters[] images, int index, Providers.MoviePosterType ptype) {
            log(Level.Debug, "Saving " + ptype.ToString().Replace("_", " ") + " as " + filename);

            if (!File.Exists(directory + "\\" + filename) || options.Overwrite == true) {
                try {
                    if (images == null)
                        images = tmdb.GetPosters(m.ID, ptype);

                    if (images != null) {
                        images[index].SavePoster(directory + "\\" + filename);
                    } else
                        log(Level.Warn, "No " + ptype.ToString().Replace("_", " ") + "s Found");
                } catch (Exception ex) {
                    log(Level.Warn, ex.Message);
                }
            } else
                log(Level.Info, ptype.ToString().Replace("_", " ") + " already exists, skipping");
        }
    }


}