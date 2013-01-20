using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbmcScout.Core;
using XbmcScout.Core.Providers;

namespace XbmcScout {

    public class Program {

        private static IMovieMetadataProvider movieProvider = new TheMovieDBProvider(Message);

        public static void Main(string[] args) {

            string dir = args[0];
            if (string.IsNullOrWhiteSpace(dir)) {
                throw new ArgumentNullException("dir");
            }

            // 1. try to match directory name to movie via api call to the movie db
            var results = movieProvider.Search(dir);

            // 2. if one match, use it; otherwise display matching files and let user select best match
            MovieXML selected = null;
            if (results.Length > 0) {
                if (results[0].ID == null) {
                    //If the result is 'empty', don't allow it to process
                    selected = null;
                } else if (results.Length == 1) {
                    //If there is only one result, skip the selection dialog
                    selected = results[0];
                } else {
                    // TODO: display selection prompt
                    
                }
            }

            while (selected == null) {
                // TODO: no results, ask user to refine or broaden their search terms.
                // ie if a folder is "007 - From russia with love", it may not match "From Russia With Love"
            }

            FetchMovie(selected);
        }

        public static void Message(String msg, XbmcScout.Core.MediaScoutMessage.MessageType mt, int level) {
            Console.WriteLine("[{0}] {1}", mt.ToString(), msg);
        }


        /// <summary>
        /// Fetches the selected movie
        /// </summary>
        private static void FetchMovie(MovieXML selected) {
            //Fetch all the information
            if (selected != null) {
                selected = movieProvider.Get(selected.ID);

                // TODO: save meta data
                selected.SaveNFO(@"C:\Temp\movie.nfo");
            }

        }



    }
}
