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
       
        public SingleFrameViewModel Cell0 { get; set; }
        public SingleFrameViewModel Cell1 { get; set; }
        public SingleFrameViewModel Cell2 { get; set; }
        public SingleFrameViewModel Cell3 { get; set; }

        public MainWindowViewModel()
        {
            Cell0 = new SingleFrameViewModel();
            Cell1 = new SingleFrameViewModel();
            Cell2 = new SingleFrameViewModel();
            Cell3 = new SingleFrameViewModel();
            
            InitSpecificationModelAsync();

        }

        /// <summary>
        /// Prepare a cameras id and location. Wrapping a model items 
        /// </summary>
        private async Task InitSpecificationModelAsync()
        {
            try
            {
                _specModel = await SpecificationModel.CreateAsync();
                _specModel.InitCameras();

                foreach (Camera c in _specModel.Cameras)
                    Cell0.Cameras.Add(new CameraViewModel(c));
            }
            catch(InvalidOperationException e)
            {
                Cell0.ErrorMessage = e.Message;
                System.Diagnostics.Debug.WriteLine(e.Message);

            }


        }






    }
}
