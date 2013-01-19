using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MediaScout
{
    public class MoveFileFunc
    {
        public FileInfo MoveFile(FileInfo src, String dest, bool overwrite)
        {
            try
            {
                if (!File.Exists(dest))
                    src.MoveTo(dest);
                else
                {
                    if (overwrite || System.Windows.Forms.MessageBox.Show("Do you want to overwrite " + src + " with " + dest, "MediaScout", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        File.Delete(dest);
                        src.MoveTo(dest);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return src;
        }

        public bool MoveFile(String src, String dest, bool overwrite)
        {
            bool success = false;
            FileInfo srcfi = new FileInfo(src);
            srcfi = MoveFile(srcfi, dest, overwrite);
            if (srcfi.FullName != src)
                success = true;
            return success;
        }
    }
}
