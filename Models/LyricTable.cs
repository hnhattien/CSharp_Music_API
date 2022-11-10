using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class LyricTable
    {
        public int id { get; set; }
        public int songid { get; set; }
        public string lyrics { get; set; }
        public Int64 userid { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
