using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace copy_app
{
    [XmlRoot]
    public class History 
    {
        [XmlElement]
        public List<HashHistory> HashHistories { get; set; } = [];
    }
    
    public class HashHistory
    {
        [XmlElement]
        public string? FilePath { get; set; }

        [XmlElement]
        public byte[]? FileHash { get; set; }
    }
}
