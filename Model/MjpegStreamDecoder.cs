using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace MJPEGStreamPlayer.Model
{
    /// <summary>
    /// Listen asynchronously server and parse HTTP a response retriving a jpeg.
    /// IMPORTANT NOTE: See remarks.
    /// </summary>
    /// <remarks>
    /// This  implemetation a parsing algorithm sensitive to a default a character set.
    /// Multi encoding and decoding a buffer data employed by
    /// a string class methods purposed for retrieving HTTP  header fields.
    /// Realized aproach is correct in .NET Framework 4.7.2
    /// but failing on .NET 3.1 at same Microsoft workbench. 
    /// Problem with a byte mangle that it not presented in UTF.
    /// Suggested solving is  force .NET to interpret the textfile
    /// as high-ANSI encoding, by telling it the codepage is 1252: 
    /// use System.Text.Encoding.GetEncoding(1252) object 
    /// in a stream read/write operation for encoding and decoding accordingly
    /// </remarks>
    class MjpegStreamDecoder
    {
        private static int _instanceCounter = 0;
        private string _streamName;

        private int _maxConnectionAttemptCounter = 10;
        private int _attempt = 10;
        private int _delay = 3000; // Delay in ms for connection

        private ulong _recievedBytes = 0;
        private DateTime _startTime;

        private uint _counter = 0;
        private int _chunkMaxSize;      // Buffer size
        private int _frameLength = 0; // A field header Content-Length
        private string _boundary; // From a frame header Content-Type: multipart/x-mixed-replace;boundary=<key>
        private bool _isConnected = false; // Partition a response header
        private byte[] _streamBuffer;
        private byte[] _headerBuffer;
        private int _streamLength;

        private byte[] _frameBuffer;
        private int _frameCurrentLength = 0 ;
        private int _frameBufferSize = 1024 * 1024;

        //TODO: Implement a frame buffer as byte[] to best performance
        private List<byte> _frame = new List<byte> { };  // Cached image

        public event EventHandler<FrameRecievedEventArgs> RaiseFrameCompleteEvent;

        public bool Active { get { return _isConnected; } set { _isConnected = value; } }
        public int Attempt
        {
            get
            {
                if (_attempt == 0)
                    _attempt = _maxConnectionAttemptCounter;
                return _attempt--;
            }
            set
            {
                _maxConnectionAttemptCounter = value;
                _attempt = _maxConnectionAttemptCounter;
            }
        }

        public int Delay { get; set; }

        public ulong RecievedBytes { get { return _recievedBytes; } }

        //TODO: Best calculation of average FPS
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
        public uint Total
        {
            get { return _counter; }
        }

        public MjpegStreamDecoder(int bufferSize = 1024)
        {
            _streamName = $"{_instanceCounter}";
            _instanceCounter++;
            _startTime = DateTime.Now;
            _chunkMaxSize = bufferSize;
            _headerBuffer = new byte[0];
            _streamBuffer = new byte[_chunkMaxSize];

            AllocateFrameBufferMemory();

        }

        /// <summary>
        /// Find a boundary in a header field and save a stream chunk byte to a frame buffer.
        /// At a frame complete raise an event with added handlers
        /// </summary>
        /// <param name="url">URL of the http stream</param>
        /// <param name="token">Cancellation token used to cancel listen server</param>
        /// <returns></returns>
        public async Task StartAsync(string url, CancellationToken? token = null)
        {
            var tok = token ?? CancellationToken.None;
            tok.ThrowIfCancellationRequested();

            //TODO: Detailed exception handling
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (Stream stream = await client.GetStreamAsync(url).ConfigureAwait(false))
                    {
                        while (true)
                        {
                            _streamLength = await stream.ReadAsync(_streamBuffer, 0, _chunkMaxSize, tok).ConfigureAwait(false);
                            ParseStreamBuffer();
                        };
                    }
                }
            }
            catch (HttpRequestException e)
            {
                //TODO: Subcribe crash event handler
            }
            finally
            {
                // Retry connection
                //Task.Delay(Delay).ContinueWith(t => StartAsync(url));
            }


        }

        private void AllocateFrameBufferMemory()
        {
            _frameBuffer = new byte[_frameBufferSize];
        }

        /// <summary>
        /// From buffer parse a response header and crop to proceed a multipart data
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
            // NOTE: Additional memory allocation. 
            // Pros is exclude overwriting buffer generating artifacted image. 
            // Cons is a expansive memory using.
            // A small a stream buffer improve performance
            byte[] chunk = new byte[_streamLength];
            Array.Copy(_streamBuffer, 0, chunk, 0, _streamLength);

            if (!_isConnected)
                GetBoundary(chunk);

            if (isEmptyHeaderBuffer())
                FindJpeg(chunk);
            else
            {
                // Case a frame header corrupt. Prepend odd chunk
                // Prevent a frame drop. Bottleneck place again.
                byte[] enhancedBuffer = Combine(_headerBuffer, chunk);
                // Empty a header buffer to avoid infinite attempt
                // to parsing already processed data item
                EmptyHeaderBuffer();
                FindJpeg(enhancedBuffer);
            }

        }
        /// <summary>
        /// Differ a header segment with jpeg part. Divide and conquer idea.
        /// </summary>
        /// <param name="data">Buffered data</param>
        private void FindJpeg(byte[] data)
        {
            if (data.Length == 0)
            {
                EmptyHeaderBuffer();
                return;
            }

            if (_frameLength == 0)  // Examine if we have to go to next a frame
            {
                if (IsHeaderFull(data))
                {
                    EmptyHeaderBuffer();
                    HeaderParse(data);      // At now we know size of image
                    (byte[] header, byte[] remains) = GetHeaderAndRemains(data);
                    FindJpeg(remains);
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
                int remainingBytes = _frameLength - _frame.Count;
                //int remainingBytes = _frameLength - _frameCurrentLength;
                int byteCounter = 0;

                
                while (byteCounter < data.Length && byteCounter < remainingBytes)
                    _frame.Add(data[byteCounter++]);
                

                /*
                while (byteCounter < data.Length && byteCounter < remainingBytes)
                {
                    _frameBuffer[_frameCurrentLength + byteCounter] = data[byteCounter];
                    byteCounter++;
                }
                */

                
                if (_frame.Count == _frameLength)
                    OnFrameCompleted();
                

                /*
                if (_frameCurrentLength == _frameLength)
                    OnFrameCompleted();
                */

                // If something left in a buffer
                // Reduce data and proceed
                byte[] reducedData = new byte[data.Length - byteCounter];
                Array.Copy(data, byteCounter, reducedData, 0, reducedData.Length);
                _headerBuffer = reducedData;    // Memorized if a header chopped
                FindJpeg(reducedData);
            }


        }

        /// <summary>
        ///  Header is not fragmented
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        /// <returns></returns>
        private bool IsHeaderFull(byte[] data)
        {
            string sData = Encoding.Default.GetString(data);
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
            string sData = Encoding.Default.GetString(data);
            string[] fields = sData.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (string field in fields)    // Helpfull data about frame
            {
                if (field.StartsWith("Content-Length:"))    // Get a frame size
                {
                    string sSize = field.Split(' ')[1];

                    try
                    {
                        _frameLength = Int32.Parse(sSize);
                    }
                    catch (FormatException)
                    {
                        //  TODO: Throw exception above
                        Console.WriteLine("Input string is invalid.");
                    }
                }
            }

        }

        private (byte[], byte[]) GetHeaderAndRemains(byte[] data)
        {
            string sData = Encoding.Default.GetString(data);
            string[] sDataParts = sData.Split(new[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
            if (sDataParts.Length == 2)
                return (Encoding.Default.GetBytes(sDataParts[0]),
                        Encoding.Default.GetBytes(sDataParts[1])); // JPEG binary part data
            else
                return (Encoding.Default.GetBytes(sDataParts[0]),
                        new byte[0]); // Empty remaining data
        }

        /// <summary>
        /// Parse a response header.Insure connection status 200. Reveal a boundary token
        /// </summary>
        /// <param name="data">Array of buffered bytes</param>
        private void ResponseHeaderParse(byte[] data)
        {
            string sHeader = Encoding.Default.GetString(data);
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
        /// <param name="data"></param>
        private void GetBoundary(byte[] data)
        {
            string sHeader = Encoding.Default.GetString(data);
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

        /// <summary>
        /// Concatenate two arrays of bytes in new one
        /// </summary>
        /// <param name="first">First sequence of bytes</param>
        /// <param name="second">Second sequence of bytes</param>
        /// <returns></returns>
        private byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        private void EmptyHeaderBuffer()
        {
            _headerBuffer = new byte[0];
        }

        private bool isEmptyHeaderBuffer()
        {
            return _headerBuffer.Length == 0;
        }

        /// <summary>
        /// Event raise and reset parameters 
        /// </summary>
        private void OnFrameCompleted()
        {
            byte[] f = _frame.ToArray();
            //byte[] f = new byte[_frameCurrentLength];
            //Array.Copy(_frameBuffer, 0, f, 0, f.Length);


            RaiseFrameCompleteEvent?.Invoke(this, new FrameRecievedEventArgs(f));

            _frame.Clear();

            _frameCurrentLength = 0;
            _counter++;
            _frameLength = 0;

        }
               

        /// <summary>
        /// Save image on disk
        /// </summary>
        /// <param name="buffer">Byte array of image</param>
        /// <param name="fileName">New a file name</param>
        /// <param name="folderName">Directory</param>
        public static void Dump(byte[] buffer, string fullName)
        {
            using (FileStream stream = File.Open(fullName, FileMode.Create))
            {
                using (BinaryWriter binWriter = new BinaryWriter(stream, Encoding.Default))
                {
                    binWriter.Write(buffer);
                }
            }

        }


    }


}
