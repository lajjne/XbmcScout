using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbmcScout.Core;
using XbmcScout.Core.Providers;

namespace XbmcScout {

    public class Program {

        private static IMovieMetadataProvider movieProvider = new TheMovieDBProvider(Debug);

        public static void Main(string[] args) {
            bool show_help = false;
            bool tvsearch = false;
            bool debug = false;

            var set = new OptionSet() {
                { "t|tv", "scan for tv shows instead of movies", v => tvsearch = v != null },
                { "d|debug", "log debug mesages", v => debug = v != null },
                { "?|h|help", "show this message and exit", v => show_help = v != null },
            };

            List<string> extra;
            try {
                extra = set.Parse(args);
            } catch (OptionException e) {
                Console.Write("xbmcscout: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'xbmcscout /?' for more information.");
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
                return;
            }

            // get sub directories
            var dirs = dir.GetDirectories();
            string what = tvsearch ? "TV Shows" : "Movies";
            if (dirs.Length <= 0) {
                Console.WriteLine("No " + what + " found in " + dir);
                return;
            } else {
                // list subdirs
                Console.WriteLine("Found " + dirs.Length + " folders to scan for " + what);
                Console.WriteLine();
                for (int i = 0; i < dirs.Length; i++) {
                    Console.WriteLine((i + 1) + ". " + dirs[i].Name);
                }
                Console.WriteLine();
                Console.WriteLine("Enter directory number or 0 to scan all directories:");
                Console.Write("> ");
                int index;
                while (!int.TryParse(Console.ReadLine(), out index) || index > dirs.Length) {
                    Console.Write("> ");
                }

                if (index == 0) {
                    foreach (var d in dirs) {
                        FetchMovie(Select(d), d);
                    }
                } else {
                    FetchMovie(Select(dirs[index - 1]), dirs[index - 1]);
                }
            }
        }

        static void ShowHelp(OptionSet p) {
            Console.WriteLine("Usage: xbmcscout [OPTIONS] directory_name");
            Console.WriteLine("Scans a directory of Movies or TV Shows and downloads XBMC meta data.");
            Console.WriteLine("Each Movie and TV Show should be in separate subdirectory.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        public static void Debug(String msg, XbmcScout.Core.MediaScoutMessage.MessageType mt, int level) {
            Console.WriteLine("[{0}] {1}", mt.ToString(), msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static MovieXML Select(DirectoryInfo dir, string name = null) {
            // get name of movie to search for
            name = name ?? dir.Name;

            // try to match directory name to movie via api call to the movie db
            var results = movieProvider.Search(name);

            // if one match, use it; otherwise display matching files and let user select best match
            MovieXML selected = null;
            if (results != null && results.Length > 0) {
                if (results[0].ID == null) {
                    // if the result is 'empty', don't allow it to process
                    selected = null;
                } else if (results.Length == 1) {
                    // if there is only one result, skip the selection dialog
                    selected = results[0];
                } else {
                    // display selection prompt
                    Console.WriteLine("Found " + selected + " matching movies. Select best match:");
                    Console.WriteLine();
                    for (int i = 0; i < results.Length; i++) {
                        Console.WriteLine(string.Format("{0}. {1} ({2})", i + 1, results[i].Title, results[i].Year));
                    }
                    Console.WriteLine();
                    Console.Write("> ");
                    int index;
                    while (!int.TryParse(Console.ReadLine(), out index) || index > results.Length) {
                        Console.Write("> ");
                    }
                    selected = results[index];
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
        /// Fetches all information for the the movie in the specified directory
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="dir"></param>
        private static void FetchMovie(MovieXML selected, DirectoryInfo dir) {
            // fetch all information for the selected movie
            if (selected != null) {
                selected = movieProvider.Get(selected.ID);

                if (selected != null) {

                    var scout = new MovieScout(new MovieScoutOptions(), Debug) { m = selected };

                    // save .nfo file
                    //selected.SaveNFO(dir.FullName);
                    scout.ProcessDirectory(dir.FullName);
                    
                }
            }

        }



    }
}
