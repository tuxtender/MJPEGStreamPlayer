using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MJPEGStreamPlayer.ViewModel;

namespace MJPEGStreamPlayer.View
{
    /// <summary>
    /// Interaction logic for SingleFrameView.xaml
    /// </summary>
    public partial class SingleFrameView : UserControl
    {
        public SingleFrameView()
        {
            InitializeComponent();
        }

        private void cameraBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cameraBox.SelectedItem != null)
            {

                var cvm = (CameraViewModel)cameraBox.SelectedItem;
                var sfvm = (SingleFrameViewModel)DataContext;

                sfvm.ChangeCamera(cvm);
            }

        }
    }
}
