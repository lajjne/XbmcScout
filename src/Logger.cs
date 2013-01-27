using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout {

    public delegate void Log(Level level, string msg);

    public enum Level {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }
}
