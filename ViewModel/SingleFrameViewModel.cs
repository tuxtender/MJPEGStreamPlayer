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
        private MjpegStreamDecoder _stream;
        private SpecificationModel _specModel;
        private ObservableCollection<CameraViewModel> _cameras;
        private BitmapImage _bitmap;
        private CancellationTokenSource _cts;

        public ObservableCollection<CameraViewModel> Cameras { get; set; }
        public BitmapImage Frame { get { return _bitmap; } }
        public bool Error 
        {
            get { return _isError; }
            private set 
            {
                _isError = value;
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(Error));
            }
        }
        public string ErrorMessage { get; private set; }
       

        public SingleFrameViewModel()
        {
            const string url = "http://demo.macroscop.com:8080/mobile?login=root&channelid=2016897c-8be5-4a80-b1a3-7f79a9ec729c&resolutionX=640&resolutionY=480&fps=25";
            _cameras = new ObservableCollection<CameraViewModel>();

            SetStartScreen();
            MakeDummyItem();

            InitSpecificationModelAsync();

            _stream = new MjpegStreamDecoder();
            _stream.RaiseFrameCompleteEvent += HandleFrameRecieved;
            //_stream.RaiseStreamFailedEvent += HandleStreamError;

            _cts = new CancellationTokenSource();

            _isError = false;

        }

        /// <summary>
        /// Prepare a cameras id and location. Wrapping a model items 
        /// </summary>
        private async Task InitSpecificationModelAsync()
        {
            _specModel = await SpecificationModel.CreateAsync();
            _specModel.InitCameras();
            foreach (Camera c in _specModel.Cameras)
                _cameras.Add(new CameraViewModel(c));

        }

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
        }

        public async void ChangeCamera(CameraViewModel cameraVM)
        {
            CloseStream();

            if(cameraVM.Name == "None")
            {
                SetStartScreen();
                return;
            }

            UriBuilder url = SpecificationModel.GetUriSelectedCamera(cameraVM.Model);

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            
            try
            {
                await _stream.StartAsync(url.ToString(), token);
            }
            catch (OperationCanceledException)
            {
                //TODO:
            }
            catch (IOException e)
            {
                ErrorNotify("Connection error");
            }

        }
    

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
            ErrorNotify("Connection error");
        }

        private void ErrorNotify(string userErrMsg)
        {
            ErrorMessage = userErrMsg;
            Error = true;
        }


    }


}

