using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels;

namespace SerialPortDevicesTestEnvironment.ViewModels.MainViewModels
{
    public class MainViewModel
    {
        public DevicesViewModel DevicesVM { get; set; }

        public MainViewModel()
        {
            DevicesVM = new DevicesViewModel();
        }
    }
}
