﻿using System.IO.Ports;
using System.Collections.ObjectModel;
using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Services;
using System.Windows.Input;

namespace SerialPortDevicesTestEnvironment.Models.Device
{
    public class Device : BindableBase
    {
        private readonly SerialPortsManager _manager;
        private CancellationTokenSource _autoSendTokenSource;

        // Giden mesajları kontrol eden komutlar
        public ICommand SendMessageCommand { get; }
        public ICommand AutoSendMessageCommand { get; }

        public Device(SerialPortsManager manager, string portName, bool isConnected)
        {
            _manager = manager;
            PortName = portName;
            IsConnected = isConnected;
            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            AutoSendMessageCommand = new RelayCommand(AutoSend);
        }

        // === NORMAL MESAJ GÖNDERME ===
        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(Interface.OutgoingMessage))
            {
                _manager.SendMessage(PortName, Interface.OutgoingMessage);
            }
        }

        private bool CanSendMessage()
        {
            return IsConnected && !string.IsNullOrWhiteSpace(Interface.OutgoingMessage);
        }

        // === OTOMATİK GÖNDERME ===
        private bool _autoSendActive = false;
        public bool AutoSendActive
        {
            get => _autoSendActive;
            set => SetProperty(ref _autoSendActive, value);
        }

        private void AutoSend()
        {
            if (AutoSendActive)
            {
                StopAutoSend();
            }
            else
            {
                StartAutoSend();
            }
        }

        private void StartAutoSend()
        {
            if (!CanSendMessage()) return;

            AutoSendActive = true;
            _autoSendTokenSource = new CancellationTokenSource();
            CancellationToken token = _autoSendTokenSource.Token;

            Task.Run(async () =>
            {
                while (AutoSendActive && !token.IsCancellationRequested)
                {
                    _manager.SendMessage(PortName, Interface.OutgoingMessage);
                    await Task.Delay(10, token); // 10ms bekle
                }
            }, token);
        }

        public void StopAutoSend()
        {
            AutoSendActive = false;
            _autoSendTokenSource?.Cancel();
        }

        // Gelen mesajları tutacağımız koleksiyon
        private ObservableCollection<DeviceMessage> _messages = new ObservableCollection<DeviceMessage>();
        public ObservableCollection<DeviceMessage> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        private DeviceInterface _interface = new DeviceInterface();
        public DeviceInterface Interface
        {
            get => _interface;
            set => SetProperty(ref _interface, value);
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

        public int sampleCount = 0;

        public DateTime lastUpdate = DateTime.Now;

        private int _dataSamplingFrequency;
        public int DataSamplingFrequency
        {
            get => _dataSamplingFrequency;
            set => SetProperty(ref _dataSamplingFrequency, value);
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
    }
}
