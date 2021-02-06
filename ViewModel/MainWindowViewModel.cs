using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJPEGStreamPlayer.ViewModel
{
    class MainWindowViewModel : NotifyPropertyChangedBase
    {
       
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
        }
    }
}
