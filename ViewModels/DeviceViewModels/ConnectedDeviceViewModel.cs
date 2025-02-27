using SerialPortDevicesTestEnvironment.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels
{
    public class ConnectedDeviceViewModel : BindableBase
    {
        private string _portName;
        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }
        // Cihazdan gelen ham metin satırlarını tutacağız
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        private string _messagesString;
        public string MessagesString
        {
            get => _messagesString;
            set => SetProperty(ref _messagesString, value);
        }

        public ConnectedDeviceViewModel(string portName)
        {
            PortName = portName;

            Messages.CollectionChanged += (s, e) =>
            {
                // Her ekleme olduğunda tüm listeyi birleştirip tek string yapalım:
                MessagesString = string.Join(Environment.NewLine, Messages);
            };
        }
    }
}
