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
using System.Globalization;

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

        private void StartStream(DateTime? start = null)
        {
            if (cameraBox.SelectedItem != null)
            {
                var cvm = (CameraViewModel)cameraBox.SelectedItem;
                var sfvm = (SingleFrameViewModel)DataContext;
                sfvm.ChangeStream(cvm, start);
            }
        }

        private void cameraBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartStream();
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            StartStream();
        }

        private void fragmentList_SelectionChanged(object sender, SelectionChangedEventArgs evnt)
        {
            var afvm = (ArchiveFragmentViewModel)fragmentList.SelectedItem;
            if (afvm == null)
                return;

            try
            {
                DateTime time = DateTime.Parse(afvm.FromTime).ToUniversalTime();
                StartStream(start: time);
            }
            catch (FormatException e)
            {
                string msg = "Failed: archive unreachable. " + e.Message;
                System.Diagnostics.Debug.WriteLine(msg);
            }

        }

        private void archiveButton_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                DateTime date = (DateTime)dateArchive.SelectedDate;
                string template = "H:m:s";

                DateTime inputTime = DateTime.ParseExact(timeBox.Text, template, CultureInfo.InvariantCulture);
                TimeSpan span = new TimeSpan(inputTime.Hour, inputTime.Minute, inputTime.Second);
                DateTime time = date.Add(span).ToUniversalTime();

                if (time > DateTime.Now.ToUniversalTime())
                {
                    throw new InvalidOperationException();
                }

                timeBox.Text = time.ToLocalTime().ToString("HH:mm:ss");

                StartStream(start: time);

            }
            catch (FormatException e)
            {
                timeBox.Text = "00:00:00";
                string msg = "Failed: Invalid time input. " + e.Message;
                System.Diagnostics.Debug.WriteLine(msg);
            }
            catch (InvalidOperationException e)
            {
                timeBox.Text = "00:00:00";
                string msg = "Failed: Suggested an invalid a archive's date. ";
                System.Diagnostics.Debug.WriteLine(msg);
            }

        }
    

    }


}
