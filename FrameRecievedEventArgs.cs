using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJPEGStreamPlayer
{
    /// <summary>
    /// Hold a image and additional info grabbed from a stream HTTP header
    /// </summary>
    public class FrameRecievedEventArgs : EventArgs
    {
        public FrameRecievedEventArgs(MemoryStream stream)
        {
            FrameStream = stream;
        }

        public MemoryStream FrameStream { get; }
    }
}
