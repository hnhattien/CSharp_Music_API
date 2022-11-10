using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class User
    {
        public Int64 id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string displayedName { get; set; }
        public string avatar { get; set; }
        public string role { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}

