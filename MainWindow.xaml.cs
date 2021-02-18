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


namespace MJPEGStreamPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void changeServer_Click(object sender, RoutedEventArgs e)
        {
            var mv = (MainWindowViewModel)DataContext;
            await mv.InitSpecificationModelAsync(url.Text);
            singleFrame.cameraBox.SelectedIndex = 0;
        }


    }


}
