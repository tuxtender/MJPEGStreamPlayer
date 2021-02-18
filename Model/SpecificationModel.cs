using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml.XPath;

namespace MJPEGStreamPlayer.Model
{
    /// <summary>
    /// Specification information about a service like cameras id and location.
    /// </summary>
    /// <remarks>
    /// Factory method design pattern implemented for call async method for initiating instance fields.
    /// Solution of a calling Task-based asynchronous methods from class constructors
    /// </remarks>
    class SpecificationModel
    {
        private readonly string _domain;
        private readonly string _url;

        private XmlDocument _doc;
        private List<Camera> _cameras;
        
        public List<Camera> Cameras { get { return _cameras; } }

        public SpecificationModel(string url)
        {
            (_domain, _url) = ParseServerUrl(url);

        }

        /// <summary>
        /// A static creation method, making the type its own factory
        /// </summary>
        /// <param name="url">Server address</param>
        /// <returns></returns>
        public static Task<SpecificationModel> CreateAsync(string url)
        {
            try
            {
                var ret = new SpecificationModel(url);
                return ret.InitializeAsync();
            }
            catch(InvalidOperationException e)
            {
                throw e;
            }
           
        }

        private async Task<SpecificationModel> InitializeAsync()
        {
            try
            {
                _doc = await GetXmlDocAsync(_url);
                return this;
            }
            catch(InvalidOperationException e)
            {
                throw e;
            }
        }

        public static async Task<XmlDocument> GetXmlDocAsync(string url)
        {
            XmlDocument doc = new XmlDocument();

            //TODO: Create a HttpClient instance using HttpClientFactory or Typed HttpClient objects
            // to no spawned a new socket instance
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    doc.LoadXml(responseBody);
                }
                catch (XmlException e)
                {
                    // Parse error in the XML
                    string msg = "Failed: cameras unreachable. " + e.Message;
                    throw new InvalidOperationException(msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                }
                catch (ArgumentNullException e)
                {
                    // The requestUri is null.
                    string msg = "Failed: cameras unreachable. " + e.Message;
                    throw new InvalidOperationException(msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                }
                catch (InvalidOperationException e)
                {
                    // The requestUri must be an absolute URI or BaseAddress must be set.
                    string msg = "Failed: cameras unreachable. " + e.Message;
                    throw new InvalidOperationException(msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                }
                catch (HttpRequestException e)
                {
                    // The request failed due to an underlying issue such as network connectivity,
                    // DNS failure, server certificate validation or timeout (only .NET Framework)
                    string msg = "Failed: cameras unreachable. " + e.Message;
                    throw new InvalidOperationException(msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                }
               
            }

            return doc;

        }

        /// <summary>
        /// Process downloaded XML to retrieve cameras identity
        /// </summary>
        public void InitCameras()
        {
            try
            {
                // XPath querying available cameras according a data sheet
                XmlNodeList childnodes = _doc.SelectNodes("//Channels/ChannelInfo");
                _cameras = new List<Camera>();

                foreach (XmlNode n in childnodes)
                {
                    string name = n.SelectSingleNode("@Name").Value;
                    string id = n.SelectSingleNode("@Id").Value;
                    _cameras.Add(new Camera(name, id));
                }
            }
            catch(XPathException e)
            {
                throw new InvalidOperationException("Failed: Not compatible server response protocol. " + e.Message);
            }

        }

        public string GetStreamRequestUrl(Camera camera, DateTime? time = null)
        {
            UriBuilder uriBuilder = new UriBuilder("http", _domain, 8080, "mobile");

            var dict = new Dictionary<string, string>()
            {
                { "login", "root"},
                { "channelid", camera.Id},
                { "resolutionX", "640"},
                { "resolutionY", "480"},
                { "fps", "25"},
                //{"password", null },
                //{"sound", "off" },
                //{"speed",  "1"},
                //{"channel",  camera.Name},
            };

            if (time.HasValue)
            {
                string template = "dd.MM.yyyy+HH:mm:ss";
                dict["mode"] = "archive";
                dict["startTime"] = time?.ToString(template);

            }

            using (var content = new FormUrlEncodedContent(dict))
            {
                uriBuilder.Query = content.ReadAsStringAsync().Result;
            }

            return Uri.UnescapeDataString(uriBuilder.ToString());

        }

        public string GetArchiveUrl(Camera camera, DateTime from, DateTime until)
        {
            string templateDate = "dd.MM.yyyy";
            string templateTime = "HH:mm:ss";

            string sFrom = from.ToString(templateDate) + "%20" + from.ToString(templateTime);
            string sUntil = until.ToString(templateDate) + "%20" + until.ToString(templateTime);

            UriBuilder uriBuilder = new UriBuilder("http", _domain, 8080, "archivefragments");

            var dict = new Dictionary<string, string>()
            {
                { "channelid", camera.Id},
                { "fromtime", sFrom},
                { "totime", sUntil},
                { "login", "root"},

            };

            using (var content = new FormUrlEncodedContent(dict))
            {
                uriBuilder.Query = content.ReadAsStringAsync().Result;
            }

            return Uri.UnescapeDataString(uriBuilder.ToString());

        }

        private (string domain, string normalizedUrl) ParseServerUrl(string url)
        {
            try
            {
                Uri u = new Uri(url);
                string domain = u.Host;
                int port = u.Port;
                UriBuilder uriBuilder = new UriBuilder("http", domain, port, "configex");
                uriBuilder.Query = "login=root";
                string normalizedUrl = uriBuilder.ToString();

                return (domain, normalizedUrl);
            }
            catch (UriFormatException e)
            {
                throw new InvalidOperationException("Failed: Invalid server address. " + e.Message);
            }

        }


    }


}
