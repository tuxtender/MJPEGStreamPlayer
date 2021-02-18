using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using MJPEGStreamPlayer.Model;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
using System.Xml;



namespace MJPEGStreamPlayer.ViewModel
{
    class SingleFrameViewModel : NotifyPropertyChangedBase
    {
        private SpecificationModel _spec;

        private bool _isError;
        private string _errorMessage;

        private BitmapImage _bitmap;
        private string _utcTimestamp;

        private CancellationTokenSource _cts;
        private MjpegStreamDecoder _stream;

        private ObservableCollection<CameraViewModel> _cameras;
        private ObservableCollection<ArchiveFragmentViewModel> _fragments;

        #region Property

        public ReadOnlyObservableCollection<CameraViewModel> Cameras { get; }
        public ReadOnlyObservableCollection<ArchiveFragmentViewModel> Fragments { get; }

        public BitmapImage Frame
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                OnPropertyChanged(nameof(Frame));
            }
        }
        public bool Error 
        {
            get { return _isError; }
            private set 
            {
                _isError = value;
                OnPropertyChanged(nameof(Error));
            } 
        }
        public string ErrorMessage 
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                Error = true;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public string Timestamp
        {
            get { return _utcTimestamp; }
            private set
            {
                if(value == "")
                    _utcTimestamp = "";
                else
                    _utcTimestamp = DateTime.Parse(value).ToLocalTime().ToString();

                OnPropertyChanged(nameof(Timestamp));

            }
        }


        #endregion Property

        #region Constructors

        public SingleFrameViewModel()
        {
            _cameras = new ObservableCollection<CameraViewModel>();
            Cameras = new ReadOnlyObservableCollection<CameraViewModel>(_cameras);

            Error = false;
            SetStartScreen();
            DummyInitCamera();
            _cts = new CancellationTokenSource();           // Intentional interruption
            _utcTimestamp = "";

            _fragments = new ObservableCollection<ArchiveFragmentViewModel>();
            Fragments = new ReadOnlyObservableCollection<ArchiveFragmentViewModel>(_fragments);

            _stream = new MjpegStreamDecoder();
            _stream.RaiseFrameCompleteEvent += HandleFrameRecieved;
            _stream.RaiseStreamFailedEvent += HandleStreamError;
            _stream.RaiseStreamStartEvent += HandleStreamStart;

        }

        #endregion Constructors
          
        #region ChangeStream

        public async void ChangeStream(CameraViewModel camera, DateTime? time)
        {
            CloseStream();

            if (camera.Id == null)      // Dummy camera 
            {
                Timestamp = "";
                _fragments.Clear();
                SetStartScreen();
                return;
            }

            CancellationToken token = _cts.Token;

            InitFragmentsAsync(camera);

            string url = _spec.GetStreamRequestUrl(camera.Model, time);

            try
            {
                await _stream.StartAsync(url, token);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("User cancel stream.");
            }

        }

        #endregion ChangeStream
    
        #region Idle screen

        private void SetStartScreen()
        {
            string filename = @"/Resources/title.jpg";

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(filename, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            _bitmap = bi;

            OnPropertyChanged(nameof(Frame));
        }

        #endregion Idle screen

        #region Event handler

        private void HandleFrameRecieved(object sender, FrameRecievedEventArgs e)
        {

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = e.FrameStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            Frame = bitmap;
            Timestamp = e.Timestamp;

        }
     
        private void HandleStreamError(object sender, StreamFailedEventArgs e)
        {
            ErrorMessage = e.Error;
        }

        private void HandleStreamStart(object sender, EventArgs e)
        {
            Error = false;
        }

        #endregion Event handler

        #region InitFragmentsAsync

        /// <summary>
        /// Preview archive fragments
        /// </summary>
        /// <param name="camera">Selected camera</param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: Improve performance. Solve freeze behavior. 
        /// </remarks>
        private async Task InitFragmentsAsync(CameraViewModel camera)
        {
            try
            {
                _fragments.Clear();
                camera.Model.Fragments.Clear();

                DateTime dateEnd = DateTime.Now.ToUniversalTime();
                DateTime dateStart = new DateTime(year: dateEnd.Year - 1,
                                                  month: dateEnd.Month,
                                                  day: dateEnd.Day,
                                                  hour: dateEnd.Hour,
                                                  minute: dateEnd.Minute,
                                                  second: dateEnd.Second);

                string url = _spec.GetArchiveUrl(camera.Model, dateStart, dateEnd);
                XmlDocument doc = await SpecificationModel.GetXmlDocAsync(url);
                await camera.Model.InitFragments(doc);

                foreach (ArchiveFragment f in camera.Model.Fragments)
                    _fragments.Add(new ArchiveFragmentViewModel(f.FromTime, f.ToTime));

            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

        }

        #endregion InitFragmentsAsync

        #region misc

        private void DummyInitCamera(string text = "None")
        {
            _cameras.Clear();
            CameraViewModel empty = new CameraViewModel(new Camera(name: text, id: null));
            _cameras.Add(empty);
        }

        private void CloseStream()
        {
            _cts.Cancel();
            Error = false;
            _cts = new CancellationTokenSource();
        }

        public void SetServer(SpecificationModel spec)
        {
            _spec = spec;
            InitCameras();
        }

        private void InitCameras()
        {
            DummyInitCamera();
            foreach (Camera c in _spec.Cameras)
                _cameras.Add(new CameraViewModel(c));

        }

        public void RemoveServer()
        {
            DummyInitCamera();
        }

        #endregion misc


    }


}

