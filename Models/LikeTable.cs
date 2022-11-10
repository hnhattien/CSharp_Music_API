using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class LikeTable
    {
        public int id { get; set; }
        public Int64 userid { get; set; }
        public int songid { get; set; }
    }
}
