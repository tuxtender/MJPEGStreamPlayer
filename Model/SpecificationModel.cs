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
        private readonly string _url = "http://demo.macroscop.com:8080/configex?login=root";
        private XmlDocument _doc;
        private List<Camera> _cameras;
        
        public List<Camera> Cameras { get { return _cameras; } }

        public SpecificationModel()
        {
        }

        /// <summary>
        /// A static creation method, making the type its own factory
        /// </summary>
        /// <returns></returns>
        public static Task<SpecificationModel> CreateAsync()
        {
            try
            {
                var ret = new SpecificationModel();
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
                _doc = await GetSpecInfoAsync();
                return this;
            }
            catch(InvalidOperationException e)
            {
                throw e;
            }
        }

        public async Task<XmlDocument> GetSpecInfoAsync()
        {
            XmlDocument doc = new XmlDocument();

            //TODO: Create a HttpClient instance using HttpClientFactory
            // to no spawned a new socket instance
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(_url).ConfigureAwait(false);
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

        /// <summary>
        /// Create a request URL is determined by a service provider
        /// </summary>
        /// <param name="camera">Camera class instance</param>
        /// <returns></returns>
        static public UriBuilder GetUriSelectedCamera(Camera camera)
        {
            UriBuilder uriBuilder = new UriBuilder("http", "demo.macroscop.com", 8080, "mobile");

            using (var content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "login", "root"},
                { "channelid", camera.Id},
                { "resolutionX", "640"},
                { "resolutionY", "480"},
                { "fps", "25"},
            }))
            {
                uriBuilder.Query = content.ReadAsStringAsync().Result;
            }
            return uriBuilder;

        }
     

    }


}
