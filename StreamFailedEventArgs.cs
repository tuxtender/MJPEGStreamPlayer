using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJPEGStreamPlayer
{
    public class StreamFailedEventArgs : EventArgs
    {
        public StreamFailedEventArgs(String error)
        {
            Error = error;
        }

        public String Error { get; }
    }
}
