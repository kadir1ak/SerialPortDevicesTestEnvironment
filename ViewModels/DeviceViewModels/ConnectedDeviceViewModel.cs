using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

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

        // Gelen mesajları satır satır tutuyoruz
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        // Kullanıcının cihaza göndermek istediği metin
        private string _outgoingMessage;
        public string OutgoingMessage
        {
            get => _outgoingMessage;
            set => SetProperty(ref _outgoingMessage, value);
        }

        // Tek bir manager referansı; oradan SendMessage() çağıracağız
        private readonly SerialPortsManager _manager;

        // Gönder butonu
        public ICommand SendMessageCommand { get; }

        public ConnectedDeviceViewModel(SerialPortsManager manager, string portName)
        {
            _manager = manager;
            PortName = portName;

            SendMessageCommand = new RelayCommand(SendMessage);
        }

        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(OutgoingMessage))
            {
                _manager.SendMessage(PortName, OutgoingMessage);
                //OutgoingMessage = string.Empty; // Mesaj kutusunu temizleme
            }
        }

        // Eğer UI tarafında tek bir metin halinde görmek isterseniz 
        // bir "MessagesString" property ekleyip, CollectionChanged'da birleştirebilirsiniz.
        // Burada satır satır tutmayı tercih ettik.
    }
}
