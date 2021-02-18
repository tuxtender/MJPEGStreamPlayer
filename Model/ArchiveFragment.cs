using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJPEGStreamPlayer.Model
{
    class ArchiveFragment
    {
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string Id { get; set; }

        public ArchiveFragment(string from, string until, string id = null)
        {
            FromTime = from;
            ToTime = until;
            Id = id;
        }


    }


}
