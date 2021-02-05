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

using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace MJPEGStreamPlayer.ViewModel
{
    class SingleFrameViewModel : NotifyPropertyChangedBase
    {
        private MjpegStreamDecoder _stream;
        private SpecificationModel _specModel;
        private ObservableCollection<CameraViewModel> _cameras;
        private BitmapImage _bitmap;

        private CancellationTokenSource _cts;

        public ObservableCollection<CameraViewModel> Cameras { get; set; }

        public BitmapImage Frame { get { return _bitmap; } }


        public SingleFrameViewModel()
        {
            const string url = "http://demo.macroscop.com:8080/mobile?login=root&channelid=2016897c-8be5-4a80-b1a3-7f79a9ec729c&resolutionX=640&resolutionY=480&fps=25";
            _cameras = new ObservableCollection<CameraViewModel>();

            SetStartScreen();

            CameraViewModel empty = new CameraViewModel(new Camera("None", "None"));
            _cameras.Add(empty);
            Cameras = _cameras;

            InitSpecificationModelAsync();

            _stream = new MjpegStreamDecoder();
            _stream.RaiseFrameCompleteEvent += HandleFrameRecieved;
            _cts = new CancellationTokenSource();
            

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

        public void CloseStream()
        {
            
            _cts.Cancel();

        }

        public void ChangeCamera(CameraViewModel cameraVM)
        {
            //CloseStream();
            _cts.Cancel();

            if(cameraVM.Name == "None")
            {
                SetStartScreen();
                return;
            }
            UriBuilder url = SpecificationModel.GetUriSelectedCamera(cameraVM.Model);
            //string url = "http://200.33.20.122:2007/axis-cgi/mjpg/video.cgi";

            _stream = new MjpegStreamDecoder();
            _stream.RaiseFrameCompleteEvent += HandleFrameRecieved;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            
            try
            {
                _stream.StartAsync(url.ToString(), token);
            }
            catch (OperationCanceledException)
            {
                //Console.WriteLine($"\n{nameof(OperationCanceledException)} thrown\n");
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

        public void HandleFrameRecieved(object sender, FrameRecievedEventArgs e)
        {
            using (MemoryStream stream = new MemoryStream(e.Frame))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _bitmap = bitmap;
            }

            OnPropertyChanged(nameof(Frame));
            
        }

        public void TakeScreenshot()
        {
            //byte[] data = Convert from _bitmap;
            //string filename = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm") + ".jpg";
            //MjpegStreamDecoder.Dump(data, filename);
        }

      

    }


}

