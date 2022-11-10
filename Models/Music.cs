using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class Music
    {
        public int id { get; set; }
        public string title { get; set; }
        public string audio { get; set; }
        public string thumbnail { get; set; }
        public string slug { get; set; }
        public string artist_name { get; set; }
        public int cat_id { get; set; }
        public string public_year { get; set; }
        public int artist_id { get; set; }
        public int viewcount { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
