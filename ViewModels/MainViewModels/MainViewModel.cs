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
