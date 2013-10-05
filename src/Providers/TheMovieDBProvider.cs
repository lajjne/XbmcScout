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
using TMDbLib.Objects.Search;
using TMDbLib.Client;
using System.Text.RegularExpressions;

namespace XbmcScout.Providers {

    public class TheMovieDBProvider : IMovieMetadataProvider {
        private Log _log;
        private TMDbClient _client;
        private string _baseUrl = "http://d3gtl9l2a4fn1j.cloudfront.net/t/p/original";

        public TheMovieDBProvider(string apikey, Log logger) {
            _log = logger;
            _client = new TMDbClient(apikey);
            _client.DefaultLanguage = "en";
        }

        string IMetadataProvider.Name { get { return "The Movie DB"; } }
        string IMetadataProvider.Version { get { return "3.0"; } }
        string IMetadataProvider.Url { get { return ""; } }

        /// <summary>
        /// Search for a movie
        /// </summary>
        /// <param name="MovieName"></param>
        /// <returns></returns>
        public IVideo[] Search(string MovieName) {
            if (_log != null)
                _log(Level.Debug, "Querying Movie ID for " + MovieName);

            // try to get year 
            int year = -1;
            if (Regex.IsMatch(MovieName, @"\(\d+\)$")) {
                int searchIndex = MovieName.LastIndexOf("(");
                if (!int.TryParse(MovieName.Substring(searchIndex).Trim('(', ')'), out year)) {
                    year = -1;
                }
                MovieName = MovieName.Substring(0, searchIndex).Trim();
            }

            var movies = new List<IVideo>();

            try {
                var results = _client.SearchMovie(MovieName);
                foreach (SearchMovie result in results.Results) {
                    MovieXML m = new MovieXML();
                    m.Title = result.Title;
                    m.LocalTitle = result.OriginalTitle;
                    m.ID = result.Id.ToString();
                    if (result.ReleaseDate.HasValue) {
                        m.ProductionYear = result.ReleaseDate.Value.Year.ToString();
                    }
                    movies.Add(m);
                }

                return movies.ToArray();

            } catch (Exception ex) {
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Get Movie details
        /// </summary>
        /// <param name="MovieID"></param>
        /// <returns></returns>
        public MovieXML Get(string MovieID) {
            MovieXML movie = null;

            try {
                var result = _client.GetMovie(int.Parse(MovieID));

                movie = new MovieXML();
                movie.ID = result.Id.ToString();
                movie.Title = result.Title;
                movie.OriginalTitle = result.OriginalTitle;
                if (result.ReleaseDate.HasValue) {
                    movie.ProductionYear = result.ReleaseDate.Value.Year.ToString();
                }
                movie.Tagline = result.Tagline;
                movie.Description = result.Overview;
                movie.RunningTime = result.Runtime.ToString();
                movie.Rating = result.VoteAverage.ToString();

                foreach (var g in result.Genres) {
                    movie.Genres.Add(new Genre { name = g.Name });
                }

                var casts = _client.GetMovieCasts(result.Id);
                if (casts != null) {
                    foreach (var a in casts.Cast) {
                        movie.Persons.Add(new Person() {
                            ID = a.Id.ToString(),
                            Name = a.Name,
                            Role = a.Character,
                            Thumb = _baseUrl + a.ProfilePath
                        });
                    }
                }

            } catch (Exception ex) {
                movie = null;
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }

            return movie;
        }

        /// <summary>
        /// Get Posters
        /// </summary>
        /// <param name="MovieID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Posters[] GetPosters(String MovieID, MoviePosterType type) {
            try {
                var movieImages = _client.GetMovieImages(int.Parse(MovieID));
                var images = type == MoviePosterType.Poster ? movieImages.Posters : movieImages.Backdrops;
                List<Posters> posters = new List<Posters>();
                foreach (var p in images) {
                    Posters poster = new Posters() {
                        Poster = _baseUrl + p.FilePath,
                        PosterFileName = p.FilePath.Substring(1)
                    };
                    posters.Add(poster);
                }

                if (posters.Count > 0)
                    return posters.ToArray();
            } catch (Exception ex) {
                if (_log != null)
                    _log(Level.Warn, ex.Message);
            }
            return null;
        }
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
