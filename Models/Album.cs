using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class Album
    {
        public int id { get; set; }
        public string title { get; set; }
        public int artist_id { get; set; }
        public int cat_id { get; set; }
        public string thumbnail { get; set; }
        public string slug { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
