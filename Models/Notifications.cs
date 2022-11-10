using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class Notifications
    {
        public Int64 id { get; set; }
        public string title { get; set; }
        public bool seen { get; set; }
        public DateTime time { get; set; }
        public string type { get; set; }
        public string thumbnail { get; set; }
        public string iconclasses { get; set; }
    }
}
