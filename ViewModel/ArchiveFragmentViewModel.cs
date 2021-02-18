using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJPEGStreamPlayer.ViewModel
{
    class ArchiveFragmentViewModel
    {
        public string Id { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }

        public ArchiveFragmentViewModel(string from, string until, string id = null)
        {
            FromTime = from;
            ToTime = until;
            Id = id;
        }
    }
}
