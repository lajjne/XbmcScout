using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace XbmcScout.Core.Helpers {

    public class DirectoryHelper {

        public static void CreateHiddenDirectory(String path) {
            // make all the metadata folders hidden, work item #2078
            Directory.CreateDirectory(path);
            DirectoryInfo di = new DirectoryInfo(path);
            if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                di.Attributes = di.Attributes | FileAttributes.Hidden;
        }
    }
}
