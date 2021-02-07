using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;


namespace MJPEGStreamPlayer.Model
{
    /// <summary>
    /// Listen asynchronously server and parse HTTP a response retriving a jpeg.
    /// </summary>
    /// <remarks>
    /// This  implemetation a parsing algorithm sensitive to a default a character set.
    /// Multi encoding and decoding a buffer data employed by
    /// a string class methods purposed for retrieving HTTP  header fields.
    /// .NET Core compatibility fixed.
    /// </remarks>
    class MjpegStreamDecoder
    {
        private const string CHARSET = "ISO-8859-1";    // Code page 28591 a.k.a. Windows-28591
        private static int _instanceCounter = 0;

        private readonly int _maxAttempt;
        private int _attempt;                           // Retry connections
        private readonly int _delay;                    // Delay in ms for connection
        private bool _isConnected;                      // Partition a response header

        private DateTime _startTime;

        private uint _counter;                          // Frame amount

        private int _contentLength;                     // Size of field header Content-Length
        private string _boundary;                       // From a frame header Content-Type: multipart/x-mixed-replace;boundary=<key>

        private readonly int _streamBufferMaxSize;               
        private byte[] _streamBuffer;
        private int _streamLength;

        private byte[] _headerBuffer;

        private int _frameBufferMaxSize;
        private byte[] _frameBuffer;
        private int _frameLength;

        #region Network

        public bool Active { get { return _isConnected; } }

        #endregion Network

        #region Statistics

        public ushort AverageFps
        {
            get { return (ushort)(_counter / (DateTime.Now - _startTime).TotalSeconds); }
        }

        /// <summary>
        /// Btirate MBps
        /// </summary>
        public uint Bitrate
        {
            get { return (uint)_frameLength * AverageFps; }
        }

        /// <summary>
        /// Total a frames treated
        /// </summary>
        public uint Total { get { return _counter; } }

        #endregion Statistics

        #region Events

        public event EventHandler<FrameRecievedEventArgs> RaiseFrameCompleteEvent;
        public event EventHandler<StreamFailedEventArgs> RaiseStreamFailedEvent;
        public event EventHandler RaiseStreamStartEvent;

        #endregion Events

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize">Max stream buffer size</param>
        /// <param name="reconnect">Count of try reconnect</param>
        /// <param name="delay">Delay reconnection in ms</param>
        public MjpegStreamDecoder(int bufferSize = 1024, int reconnect = 10, int delay = 5000)
        {
            _instanceCounter++;
            _startTime = DateTime.Now;
            _streamBufferMaxSize = bufferSize;
            _streamBuffer = new byte[_streamBufferMaxSize];
            _headerBuffer = null;
            _isConnected = false;
            _delay = delay;
            _maxAttempt = reconnect;
            _attempt = _maxAttempt;

            AllocateFrameBufferMemory();

        }

        #endregion Constructors

        #region StartAsync

        /// <summary>
        /// Find a boundary in a header field and save a stream chunk byte to a frame buffer.
        /// At a frame complete raise an event with added handlers
        /// </summary>
        /// <param name="url">URL of the http stream</param>
        /// <param name="token">Cancel listen server</param>
        /// <returns></returns>
        public async Task StartAsync(string url, CancellationToken? token = null)
        {
            var tok = token ?? CancellationToken.None;
            tok.ThrowIfCancellationRequested();         // User request stop stream

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (Stream stream = await client.GetStreamAsync(url).ConfigureAwait(false))
                    {
                        while (true)
                        {
                            _streamLength = await stream.ReadAsync(_streamBuffer, 0,
                                                                   _streamBufferMaxSize, tok
                                                                  ).ConfigureAwait(false);
                            ParseStreamBuffer();
                        };
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                // The requestUri is null. Only .NET 5.0
                string msg = "Failed: " + e.Message;
                RaiseStreamFailedEvent?.Invoke(this, new StreamFailedEventArgs(msg));
                System.Diagnostics.Debug.WriteLine(msg);
            }
            catch (InvalidOperationException e)
            {
                // The requestUri must be an absolute URI or BaseAddress must be set.
                string msg = "Failed: " + e.Message;
                RaiseStreamFailedEvent?.Invoke(this, new StreamFailedEventArgs(msg));
                System.Diagnostics.Debug.WriteLine(msg);
            }
            catch (HttpRequestException e)
            {
                // The request failed due to an underlying issue such as network connectivity,
                // DNS failure, server certificate validation or timeout (only .NET Framework)
                string msg = "Failed: " + e.Message;
                RaiseStreamFailedEvent?.Invoke(this, new StreamFailedEventArgs(msg));
                System.Diagnostics.Debug.WriteLine(msg);
            }
            catch (IOException e)
            {
                // Stream reading error
                string msg = "Failed: " + e.Message;
                RaiseStreamFailedEvent?.Invoke(this, new StreamFailedEventArgs(msg));
                System.Diagnostics.Debug.WriteLine(msg);
            }
            finally
            {
       
            }

            // Retry connection
            await Task.Delay(_delay);
            System.Diagnostics.Debug.WriteLine("Trying reconnect.");

            if (_attempt > 0)
            {
                _attempt--;
                await StartAsync(url, token).ConfigureAwait(false);
            }

        }

        #endregion StartAsync

        #region Parsing routine

        /// <summary>
        /// Fill frame buffer
        /// </summary>
        /// <remarks>
        /// In situation of poorly connection the early started
        /// image will being filled garbage and raise event about complition as normally
        /// then parsing new buffered stream byte chunk and drop it
        /// if cannot determine complete header of next frame 
        /// then will repeat reading from stream 
        /// </remarks>
        private void ParseStreamBuffer()
        {
            // Meaning is shrink range of index a stream buffer 
            // that avoided allocating additional memory. 
            // If a current start index and stream length is match
            // there stream buffer array has entire proceed. 
            int start = 0;

            if (!_isConnected)
            {
                _attempt = _maxAttempt;
                RaiseStreamStartEvent?.Invoke(this, new EventArgs());
                GetBoundary(_streamBuffer);
            }

            if (!isEmptyHeaderBuffer())
            {
                // Case a frame header corrupt. Prepend odd chunk
                // Prevent a frame drop. Bottleneck place.

                byte[] enhancedBuffer = Combine(_headerBuffer, _streamBuffer);
                _streamBuffer = enhancedBuffer;
                _streamLength = enhancedBuffer.Length;

                // Empty a header buffer to avoid infinite attempt
                // to parsing already processed data item
                EmptyHeaderBuffer();
            }

            ParseStreamBufferFromIndex(start);

        }

        /// <summary>
        /// Process a stream buffer from a given index until the end.
        /// </summary>
        /// <param name="index">Determine start index not already parsed stream buffer bytes</param>
        private void ParseStreamBufferFromIndex(int index)
        {
            if (index == _streamLength)
            {
                EmptyHeaderBuffer();
                return;
            }

            if (_contentLength == 0)          // Ready to init a new frame
            {
                // Need a array for string methods is core this app
                byte[] chunk = new byte[_streamLength - index];
                Array.Copy(_streamBuffer, index, chunk, 0, chunk.Length);

                if (IsHeaderFull(chunk))
                {
                    EmptyHeaderBuffer();
                    HeaderParse(chunk);      // At now we know size of image
                    (byte[] header, byte[] remains) = GetHeaderAndRemains(chunk);
                    index = _streamLength - remains.Length;
                    ParseStreamBufferFromIndex(index);
                }
                else
                    // Header is fragmented
                    // Join a fragmented header and new data read from stream
                    // Return to ParseStreamBuffer
                    return;
            }
            else
            {
                // Inside raw JPEG
                int remainsStreamBufBytes = _streamLength - index;
                int remainsFrameBytes = _contentLength - _frameLength;
                int i = 0;

                while (i < remainsStreamBufBytes && i < remainsFrameBytes)
                {
                    _frameBuffer[_frameLength++] = _streamBuffer[index++];
                    i++;
                }

                if (_frameLength == _contentLength)
                    OnFrameCompleted();

                // If something left in a buffer memorize it for extending a new streambuffer
                remainsStreamBufBytes = _streamLength - index;
                if (remainsStreamBufBytes != 0)
                {
                    byte[] cache = new byte[remainsStreamBufBytes];
                    Array.Copy(_streamBuffer, index, cache, 0, cache.Length);
                    _headerBuffer = cache;
                }
                ParseStreamBufferFromIndex(index);
            }

        }

        #endregion Parsing routine 

        #region HTTP header parsing

        /// <summary>
        ///  Header is not fragmented
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        /// <returns></returns>
        private bool IsHeaderFull(byte[] data)
        {
            string sData = Encoding.GetEncoding(CHARSET).GetString(data);
            if (sData.Contains("--" + _boundary) && sData.Contains("\r\n\r\n"))
                return true;

            return false;
        }

        /// <summary>
        /// Parse chunk of frame
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        private void HeaderParse(byte[] data)
        {
            /*  Sample a frame header:
            *  --boundary
            *  Content-Type: image/jpeg
            *      ...
            *  Content-Length: xxx
            *
            *   %Binary JPEG%
            */
            string sData = Encoding.GetEncoding(CHARSET).GetString(data);
            string[] fields = sData.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (string field in fields)    // Helpfull data about frame
            {
                if (field.StartsWith("Content-Length:"))    // Get a frame size
                {
                    string sSize = field.Split(' ')[1];

                    try
                    {
                        _contentLength = Int32.Parse(sSize);
                    }
                    catch (FormatException)
                    {
                        //  TODO: Smthg
                        // Malicious server response
                    }
                }
            }

        }

        /// <summary>
        /// Get apart header and body of a response
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        /// <returns>header and body</returns>
        private (byte[], byte[]) GetHeaderAndRemains(byte[] data)
        {
            string sData = Encoding.GetEncoding(CHARSET).GetString(data);
            string[] sDataParts = sData.Split(new[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
            if (sDataParts.Length == 2)
                return (Encoding.GetEncoding(CHARSET).GetBytes(sDataParts[0]),
                        Encoding.GetEncoding(CHARSET).GetBytes(sDataParts[1])); // JPEG binary part data

            else
                return (Encoding.GetEncoding(CHARSET).GetBytes(sDataParts[0]),
                        new byte[0]); // Empty remaining data
        }

        /// <summary>
        /// Parse a response header.
        /// Insure connection status 200. Reveal a boundary token
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        private void ResponseHeaderParse(byte[] data)
        {
            string sHeader = Encoding.GetEncoding(CHARSET).GetString(data);
            string[] headerFields = sHeader.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (string field in headerFields)
            {
                if (field.EndsWith("200 OK"))    // Connection is established
                    _isConnected = true;

                if (field.StartsWith("Content-Type: multipart")) // Determine a boundary
                {
                    Regex r = new Regex(@"boundary='?(.*)'?");
                    Match match = r.Match(field);
                    _boundary = match.Groups[1].Value;
                }
            }
        }

        /// <summary>
        /// Parse for a boundary delimiter
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        private void GetBoundary(byte[] data)
        {
            string sHeader = Encoding.GetEncoding(CHARSET).GetString(data);
            string[] headerFields = sHeader.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (string field in headerFields)
            {
                if (field.StartsWith("--")) // Determine a boundary
                {
                    _isConnected = true;
                    Regex r = new Regex(@"--'?(.*)'?");
                    Match match = r.Match(field);
                    _boundary = match.Groups[1].Value;
                }
            }

        }

        #endregion HTTP header parsing

        #region Utility

        /// <summary>
        /// Concatenate two arrays of bytes in new one
        /// </summary>
        /// <param name="first">First sequence of bytes</param>
        /// <param name="second">Second sequence of bytes</param>
        /// <returns>New a byte array</returns>
        private byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        /// <summary>
        /// Estimate max size frame buffer
        /// </summary>
        private void AllocateFrameBufferMemory()
        {
            //TODO: Suggest heuristic or deterministic algorithm figure out max size frame buffer
            _frameBufferMaxSize = _streamBufferMaxSize * _streamBufferMaxSize;
            _frameBuffer = new byte[_frameBufferMaxSize];
        }

        private void EmptyHeaderBuffer()
        {
            _headerBuffer = null;
        }

        private bool isEmptyHeaderBuffer()
        {
            return _headerBuffer == null;
        }

        #endregion Utility

        #region Completed frame's methods

        /// <summary>
        /// Event raise and reset parameters 
        /// </summary>
        private void OnFrameCompleted()
        {
            // MemoryStream implements the IDisposable interface,
            // but does not actually have any resources to dispose. 
            // directly calling Dispose() is not necessary as MSDN told.
            var s = new MemoryStream(_frameBuffer, 0, _frameLength);

            RaiseFrameCompleteEvent?.Invoke(this, new FrameRecievedEventArgs(s));

            _frameLength = 0;
            _contentLength = 0;

            _counter++;

        }

        /// <summary>
        /// Save on disk
        /// </summary>
        /// <param name="stream">Stream data to save</param>
        /// <param name="fullName">New a full filename</param>
        public static void Dump(MemoryStream stream, string fullName)
        {
            using (FileStream output = File.Open(fullName, FileMode.Create))
            {
                stream.CopyTo(output);
            }

        }

        #endregion Completed frame's methods

    }


}
