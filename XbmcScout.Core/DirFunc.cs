using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace XbmcScout {

    public class DirFunc {
        public String GetDirectoryName(String path) {
            return (new DirectoryInfo(path).Name);
        }

        public bool MergeDirectories(String src, String dest, bool overwrite) {
            bool state = false;
            if (!Directory.Exists(dest))
                Directory.Move(src, dest);
            else {
                DirectoryInfo srcdi = new DirectoryInfo(src);
                DirectoryInfo destdi = new DirectoryInfo(dest);
                if (src.ToLower() != dest.ToLower())
                    CopyDirectory(srcdi, destdi, false);
                else {
                    // TODO: ask before renaming
                    //if (overwrite || System.Windows.Forms.MessageBox.Show("Do you want to rename folder from " + srcdi.Name + " to " + destdi.Name, "MediaScout", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    //{
                    //    Directory.Move(src, dest + "temp");
                    //    Directory.Move(dest + "temp", dest);
                    //}

                }
                if ((new DirectoryInfo(src).GetFiles().Length == 0) && (new DirectoryInfo(src).GetDirectories().Length == 0)) {
                    Directory.Delete(src);
                    state = true;
                }
            }
            return state;
        }

        public void CopyDirectory(DirectoryInfo source, DirectoryInfo target, bool overwrite) {
            foreach (DirectoryInfo dir in source.GetDirectories()) {
                if (Directory.Exists(target.FullName))
                    CopyDirectory(dir, new DirectoryInfo(Path.Combine(target.FullName, dir.Name)), overwrite);
                else
                    CopyDirectory(dir, target.CreateSubdirectory(dir.Name), overwrite);
            }
            foreach (FileInfo file in source.GetFiles()) {
                String targetfile = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(targetfile))
                    file.MoveTo(targetfile);
                else {
                    // TODO: ask before overwrite
                    //if (overwrite || System.Windows.Forms.MessageBox.Show("Do you want to Overwrite " + file.FullName + " with " + targetfile, "MediaScout", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    //{
                    //    File.Delete(targetfile);
                    //    file.MoveTo(targetfile);
                    //}
                }
            }
        }

        public void CreateHiddenFolder(String FolderPath) {
            // make all the metadata folders hidden, work item #2078
            Directory.CreateDirectory(FolderPath);
            DirectoryInfo di = new DirectoryInfo(FolderPath);
            if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                di.Attributes = di.Attributes | FileAttributes.Hidden;
        }
    }
}
