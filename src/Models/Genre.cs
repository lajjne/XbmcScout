using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace XbmcScout.Models {

    /// <summary>
    /// Movie genre.
    /// </summary>
    public class Genre {
        [XmlText]
        public String name;

    }
}