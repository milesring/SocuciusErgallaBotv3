using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocuciusErgallaBotv3.Model
{
    public class TrackHistory
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string URL { get; set; }
        public int Plays { get; set; }
        public User User { get; set; }
    }
}
