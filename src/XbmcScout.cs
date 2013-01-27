using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using XbmcScout.Models;
using XbmcScout.Providers;

namespace XbmcScout {

    public class XbmcScout {

        private IMovieMetadataProvider _moviedb;
        private ITVMetadataProvider _tvdb;
        private Options _options;
        private DirectoryInfo _dir;

        private static bool debug;

        /// <summary>
        /// Main entry point. Parses command line and starts the scan.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            Options options = new Options();
            bool show_help = false;

            // declare command line options
            var set = new OptionSet() {
                { "t|tv", "scan for tv shows instead of movies", v => options.TvSearch = v != null },
                { "p|posters", "download posters", v => options.GetPosters = v != null },
                { "b|backdrops", "download backdrops", v => options.GetBackdrops = v != null },
                { "o|overwrite", "overwrite existing metadata and images", v => options.Overwrite = v != null },
                { "d|debug", "log debug messages", v => debug = v != null },
                { "?|h|help", "show this message and exit", v => show_help = v != null },
            };

            List<string> extra;
            try {
                extra = set.Parse(args);
            } catch (OptionException e) {
                Console.Write("xbmcscout: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'xbmcscout /?' for more information.");
                Console.WriteLine();
                return;
            }

            if (show_help) {
                ShowHelp(set);
                return;
            }

            DirectoryInfo dir = null;
            try {
                dir = new DirectoryInfo(string.Join(" ", extra.ToArray()));
            } catch {
                Console.Write("No directory specified. Try 'xbmcscout /?' for more information.");
                Console.WriteLine();
                return;
            }

            if (!dir.Exists) {
                Console.Write("Directory not found. Enter the path to your Movies/TV Shows directory.");
                Console.WriteLine();
                return;
            }

            // start the scan
            var scout = new XbmcScout(options, dir);
            scout.Start();
        }

        /// <summary>
        /// Display help for the program.
        /// </summary>
        /// <param name="p"></param>
        static void ShowHelp(OptionSet p) {
            Console.WriteLine("Usage: xbmcscout [options] path");
            Console.WriteLine("Scans a directory of Movies or TV Shows and downloads XBMC metadata and images.");
            Console.WriteLine("Each Movie and TV Show should be in a separate subdirectory.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Print log messages.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="mt"></param>
        /// <param name="msg"></param>
        public static void Log(Level level, string msg) {
            if (debug || level > Level.Info) {
                Console.WriteLine("[{0}] {1}", level.ToString().ToUpper(), msg);
            }
        }


        /// <summary>
        /// Initializes a new instance of the XbmcScout program with the specified options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dir"></param>
        public XbmcScout(Options options, DirectoryInfo dir) {
            _moviedb = new TheMovieDBProvider(Log);
            _tvdb = new TheTVDBProvider(Log);
            _options = options;
            _dir = dir;
        }

        /// <summary>
        /// Start the directory scan.
        /// </summary>
        private void Start() {

            // get sub directories
            var dirs = _dir.GetDirectories();
            string what = _options.TvSearch ? "TV Shows" : "Movies";
            if (dirs.Length <= 0) {
                Console.WriteLine("No " + what + " found in " + _dir);
                return;
            } else {
                // list subdirs
                Console.WriteLine("Found " + dirs.Length + " folders to scan for " + what + ":");
                Console.WriteLine();
                for (int i = 0; i < dirs.Length; i++) {
                    Console.WriteLine((i + 1) + ". " + dirs[i].Name);
                }
                Console.WriteLine();
                Console.WriteLine("Enter directory number or 0 to scan all directories:");
                Console.Write("> ");
                int index;
                while (!int.TryParse(Console.ReadLine(), out index) || index < 0 || index > dirs.Length) {
                    Console.Write("> ");
                }

                if (index == 0) {
                    foreach (var d in dirs) {
                        Process(Select(d), d);
                    }
                } else {
                    Process(Select(dirs[index - 1]), dirs[index - 1]);
                }
            }
        }

        /// <summary>
        /// Select a movie or tv show based on the specified directory name.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private IVideo Select(DirectoryInfo dir, string name = null) {
            string what = _options.TvSearch ? "TV Shows" : "Movies";

            // get name of movie/tv show to search for
            name = name ?? dir.Name;

            // try to match directory name to movie(tvshow via api call
            var results = _options.TvSearch ? _tvdb.Search(name) : _moviedb.Search(name);

            // if one match, use it; otherwise display matching files and let user select best match
            IVideo selected = null;
            if (results != null && results.Length > 0) {
                if (results[0].ID == null) {
                    // if the result is 'empty', don't allow it to process
                    selected = null;
                } else if (results.Length == 1) {
                    // if there is only one result, skip the selection dialog
                    selected = results[0];
                } else {
                    // display selection prompt
                    Console.WriteLine("Found " + results.Length + " matching " + what + ":");
                    Console.WriteLine();
                    for (int i = 0; i < results.Length; i++) {
                        Console.WriteLine(string.Format("{0}. {1} ({2})", i + 1, results[i].Title, results[i].Year));
                    }
                    Console.WriteLine();
                    Console.WriteLine("Enter number of best match:");
                    Console.Write("> ");
                    int index;
                    while (!int.TryParse(Console.ReadLine(), out index) || index <= 0 || index > results.Length) {
                        Console.Write("> ");
                    }
                    selected = results[index - 1];
                }
            }

            while (selected == null) {
                // no results, ask user to refine or broaden their search terms
                Console.WriteLine("No match for '" + name + "'. Please refine or broaden the search terms:");
                Console.Write("> ");
                while (string.IsNullOrWhiteSpace(name = Console.ReadLine())) {
                    Console.Write("> ");
                }
                selected = Select(dir, name);
            }
            return selected;
        }


        /// <summary>
        /// Process the selected movie/tvshow, i.e. donwload metadata and images.
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="tvsearch"></param>
        /// <param name="dir"></param>
        private void Process(IVideo selected, DirectoryInfo dir) {
            // fetch all information for the selected movie/tvshow
            if (selected != null) {
                if (_options.TvSearch) {
                    var show = _tvdb.GetTVShow(selected.ID);
                    if (show != null) {
                        // process tv show
                        var scout = new TVScout(show, _options, Log);
                        scout.ProcessDirectory(dir.FullName);
                    }
                } else {
                    var movie = _moviedb.Get(selected.ID);
                    if (movie != null) {
                        // process movie
                        var scout = new MovieScout(movie, _options, Log);
                        scout.ProcessDirectory(dir.FullName);
                    }
                }
            }
        }
    }
}
