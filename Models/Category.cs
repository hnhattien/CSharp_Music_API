using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class Category
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string thumbnail { get; set; }
    }
}
