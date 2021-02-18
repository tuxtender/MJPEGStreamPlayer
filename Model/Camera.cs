using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;
using System.Xml.XPath;
using System.Threading.Tasks;


namespace MJPEGStreamPlayer.Model
{
    /// <summary>
    /// All about streaming device
    /// </summary>
    class Camera
    {
        // Place here other additional info
        public List<ArchiveFragment> Fragments;
        public string Name { get; }
        public string Id { get; }

        public Camera(string name, string id)
        {
            Name = name;
            Id = id;
            Fragments = new List<ArchiveFragment>();
        }

        public async Task InitFragments(XmlDocument doc)
        {
            try
            {
                Fragments.Clear();

                XmlNodeList childnodes = doc.SelectNodes("/*/*/*");

                foreach (XmlNode n in childnodes)
                {
                    XmlNodeList fragmentNodes = n.SelectNodes("*");
                    string template = "dd.MM.yyyy HH:mm:ss";

                    string fromTime = DateTime.Parse(fragmentNodes[1].InnerText).ToLocalTime().ToString(template);
                    string toTime = DateTime.Parse(fragmentNodes[2].InnerText).ToLocalTime().ToString(template);
                    string id = fragmentNodes[0].InnerText;

                    ArchiveFragment nf = new ArchiveFragment(fromTime, toTime, id);
                    Fragments.Add(nf);

                }

            }
            catch (XPathException e)
            {
                throw new InvalidOperationException("Failed: Not compatible server response protocol. " + e.Message);
            }
            catch (FormatException e)
            {
                throw new InvalidOperationException("Failed: Fragment time not recognized. " + e.Message);
            }

        }


    }


}
