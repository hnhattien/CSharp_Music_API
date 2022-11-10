using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class ResetPassword
    {
        public int id { get; set; }
        public string selector { get; set; }
        public string token { get; set; }
        public string useremail { get; set; }
        public string expires { get; set; }
    }
}
