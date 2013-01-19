using System;
using System.Collections.Generic;
using System.Text;

namespace XbmcScout.Core {
    public class MediaScoutMessage {
        public delegate void Message(String msg, MessageType mt, int level);

        public enum MessageType {
            Task = 0,
            TaskResult = 1,
            TaskError = 2,
            Error = 3,
            FatalError = 4
        }
    }
}
