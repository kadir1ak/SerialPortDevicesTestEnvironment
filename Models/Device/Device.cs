using System.IO.Ports;
using System.Collections.ObjectModel;
using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Models.Data;
using SerialPortDevicesTestEnvironment.Services;
using System.Windows.Input;

namespace SerialPortDevicesTestEnvironment.Models.Device
{
    public class Device : BindableBase
    {
        private readonly SerialPortsManager _manager;

        // Giden mesajları kontrol eden komut
        public ICommand SendMessageCommand { get; }
        public Device(SerialPortsManager manager, string portName, bool isConnected)
        {
            _manager = manager;
            PortName = portName;
            IsConnected = isConnected;
            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
        }

        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(OutgoingMessage))
            {
                _manager.SendMessage(PortName, OutgoingMessage);
            }
        }

        private bool CanSendMessage()
        {
            return IsConnected && !string.IsNullOrWhiteSpace(OutgoingMessage);
        }

        private string _id;
        public string ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _portName;
        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private int _baudRate = 9600;
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private Parity _parity = Parity.None;
        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        private StopBits _stopBits = StopBits.One;
        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        // Gelen mesajları tutacağımız koleksiyon
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();
        public ObservableCollection<Message> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        // Kullanıcının UI'da yazıp göndereceği geçici metin
        private string _outgoingMessage;
        public string OutgoingMessage
        {
            get => _outgoingMessage;
            set => SetProperty(ref _outgoingMessage, value);
        }
    }
}
