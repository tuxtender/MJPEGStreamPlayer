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



namespace MJPEGStreamPlayer.ViewModel
{
    class SingleFrameViewModel : NotifyPropertyChangedBase
    {
        private bool _isError;
        private string _errorMessage;

        private MjpegStreamDecoder _stream;
        private ObservableCollection<CameraViewModel> _cameras;
        private BitmapImage _bitmap;
        private CancellationTokenSource _cts;

        #region Property
        public ObservableCollection<CameraViewModel> Cameras { get; private set; }
        public BitmapImage Frame { get { return _bitmap; } }
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

        #endregion Property

        #region Constructors

        public SingleFrameViewModel()
        {
            _cameras = new ObservableCollection<CameraViewModel>();
            Error = false;
            SetStartScreen();
            MakeDummyItem();
            _cts = new CancellationTokenSource();           // Intentional interruption

            _stream = new MjpegStreamDecoder();
            _stream.RaiseFrameCompleteEvent += HandleFrameRecieved;
            _stream.RaiseStreamFailedEvent += HandleStreamError;
            _stream.RaiseStreamStartEvent += HandleStreamStart;

        }

        #endregion Constructors

        #region misc
   
        private void MakeDummyItem(string text="None")
        {
            CameraViewModel empty = new CameraViewModel(new Camera(text, text));
            _cameras.Add(empty);
            Cameras = _cameras;
        }

        private void CloseStream()
        {
            _cts.Cancel();
            Error = false;
            _cts = new CancellationTokenSource();
        }

        #endregion misc

        #region ChangeCamera

        public async void ChangeCamera(CameraViewModel cameraVM)
        {
            CloseStream();

            if(cameraVM.Name == "None")
            {
                SetStartScreen();
                return;
            }

            CancellationToken token = _cts.Token;
            string url = cameraVM.Url;
            
            try
            {
                await _stream.StartAsync(url, token);
            }
            catch (OperationCanceledException)
            {
                // Stream ended no error
                System.Diagnostics.Debug.WriteLine("User cancel stream");
            }

        }

        #endregion ChangeCamera

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

            _bitmap = bitmap;

            OnPropertyChanged(nameof(Frame));
            
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

    }


}

