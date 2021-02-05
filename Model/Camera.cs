using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;


namespace MJPEGStreamPlayer.Model
{
    /// <summary>
    /// All about streaming device
    /// </summary>
    class Camera
    {
        // Place here other additional info
        public string Name { get; }
        public string Id { get; }

        public Camera(string name, string id)
        {
            Name = name;
            Id = id;
        }

    }
}
