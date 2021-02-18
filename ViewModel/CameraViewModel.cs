using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MJPEGStreamPlayer.Model;

namespace MJPEGStreamPlayer.ViewModel
{
    class CameraViewModel : NotifyPropertyChangedBase
    {
        private Camera _camera;

        public Camera Model { get { return _camera; } }
        public string Name { get { return _camera.Name; } }
        public string Id { get { return _camera.Id; } }

        public CameraViewModel(Camera camera)
        {
            _camera = camera;
        }
    }
}
