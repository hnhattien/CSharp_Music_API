using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Models
{
    public class NotificationSeen
    {
        public int id { get; set; }
        public Int64 notification_id { get; set; }
        public Int64 admin_id { get; set; }
    }
}
