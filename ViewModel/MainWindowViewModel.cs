using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MJPEGStreamPlayer.Model;

namespace MJPEGStreamPlayer.ViewModel
{
    class MainWindowViewModel : NotifyPropertyChangedBase
    {
        private SpecificationModel _specModel;
        private string _header;
       
        public string Header
        {
            get { return _header; }
            private set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public SingleFrameViewModel Cell0 { get; set; }
        public SingleFrameViewModel Cell1 { get; set; }
        public SingleFrameViewModel Cell2 { get; set; }
        public SingleFrameViewModel Cell3 { get; set; }

        public MainWindowViewModel()
        {
            Header = "MJPEGStreamPlayer";

            Cell0 = new SingleFrameViewModel();
            Cell1 = new SingleFrameViewModel();
            Cell2 = new SingleFrameViewModel();
            Cell3 = new SingleFrameViewModel();
            
            InitSpecificationModelAsync();

        }

        /// <summary>
        /// Request data about cameras and add them as soon as possible
        /// </summary>
        private async Task InitSpecificationModelAsync()
        {
            try
            {
                _specModel = await SpecificationModel.CreateAsync();
                _specModel.InitCameras();

                foreach (Camera c in _specModel.Cameras)
                {
                    Cell0.Cameras.Add(new CameraViewModel(c));
                    Cell1.Cameras.Add(new CameraViewModel(c));
                    Cell2.Cameras.Add(new CameraViewModel(c));
                    Cell3.Cameras.Add(new CameraViewModel(c));
                }

            }
            catch(InvalidOperationException e)
            {
                Header = e.Message;
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

        }


    }


}
