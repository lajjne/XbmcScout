using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace XbmcScout {
    public class Posters {
        String poster;
        String posterfilename;
        String thumb;
        String res;

        public String Poster {
            get { return poster; }
            set {
                poster = value;
                posterfilename = poster.Substring(poster.LastIndexOf("/") + 1);
            }
        }

        public String PosterFileName {
            get { return posterfilename; }
            set { posterfilename = value; }
        }

        public String Thumb {
            get { return thumb; }
            set { thumb = value; }
        }

        public String Resolution {
            get { return res; }
            set { res = value; }
        }

        public void SavePoster(String filepath) {
            savegraphic(this.poster, filepath);
        }

        public void SaveThumb(String filepath) {
            savegraphic(this.thumb, filepath);
        }

        private void savegraphic(String fileIn, String fileOut) {
            try {
                WebRequest requestPic = WebRequest.Create(fileIn);
                WebResponse responsePic = requestPic.GetResponse();

                Image temp = Image.FromStream(responsePic.GetResponseStream());
                Bitmap bmp = new Bitmap(temp);
                bmp.Save(fileOut, ImageFormat.Jpeg);

            } catch (Exception ex) {
                File.Delete(fileOut);
                throw new Exception(ex.Message, ex.InnerException);
            }
        }
    }
}
