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
        private const string TITLE = "MJPEGStreamPlayer";
        private SpecificationModel _specModel;
        private string _header;
        private string _url;

        public string Header
        {
            get { return _header; }
            private set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                OnPropertyChanged(nameof(Url));
            }
        }

        public SingleFrameViewModel Cell { get; set; }
     

        public MainWindowViewModel()
        {
            Header = TITLE;
            Url = "http://demo.macroscop.com:8080";
            Cell = new SingleFrameViewModel();
            InitSpecificationModelAsync(Url);

        }

        /// <summary>
        /// Request data about cameras and add them as soon as possible
        /// </summary>
        /// <param name="url">Streaming server url</param>
        /// <returns></returns>
        public async Task InitSpecificationModelAsync(string url)
        {
            try
            {
                _specModel = await SpecificationModel.CreateAsync(url);
                _specModel.InitCameras();

                Cell.SetServer(_specModel);
                Header = TITLE;

            }
            catch(InvalidOperationException e)
            {
                Header = e.Message;
                Cell.RemoveServer();
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

        }
      

    }


}
