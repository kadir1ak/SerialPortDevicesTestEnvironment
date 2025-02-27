using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Services;

namespace SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels
{
    public class DevicesViewModel : BindableBase
    {
        private readonly SerialPortsManager _manager;
        public ObservableCollection<ConnectedDeviceViewModel> ConnectedDevices { get; } = new ObservableCollection<ConnectedDeviceViewModel>();
        public ObservableCollection<SerialPort> ConnectedPorts => _manager.ConnectedPorts;
        public ObservableCollection<string> AvailablePorts => _manager.AvailablePorts;

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public DevicesViewModel()
        {
            _manager = new SerialPortsManager();
            _manager.MessageReceived += OnMessageReceived;
            // RelayCommand: parametresiz versiyon
            ConnectCommand = new RelayCommand(ExecuteConnect, CanExecuteConnect);
            DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect);
        }
        private void OnMessageReceived(string portName, string data)
        {
            // Hangi porttan geldiğini bul, ilgili "ConnectedDeviceViewModel" in Messages listesine ekle:
            var deviceVM = ConnectedDevices.FirstOrDefault(d => d.PortName == portName);
            if (deviceVM != null)
            {
                // Tek bir satır ekleyelim; 
                // multi-line ise satırları parçalayıp ekleyebilirsiniz
                deviceVM.Messages.Add(data);
            }
        }
        private void ExecuteConnect()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                // Dilediğiniz baud rate'i verin, örneğin 921600
                _manager.ConnectToPort(SelectedPort, 921600);
                // Bağlantı sağlandıysa, biz de “ConnectedDevices” listemize 
                // o portu temsil eden bir DeviceViewModel ekleyelim:
                // Eğer zaten yoksa ekleyelim
                if (!ConnectedDevices.Any(d => d.PortName == SelectedPort))
                {
                    ConnectedDevices.Add(new ConnectedDeviceViewModel(SelectedPort));
                }
            }
        }
        private bool CanExecuteConnect()
        {
            // Sadece seçili port varsa
            return !string.IsNullOrEmpty(SelectedPort);
        }

        private void ExecuteDisconnect()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                _manager.DisconnectFromPort(SelectedPort);
                // Bağlantı kesildikten sonra, istersek "ConnectedDevices"’dan da silebiliriz
                var deviceVM = ConnectedDevices.FirstOrDefault(d => d.PortName == SelectedPort);
                if (deviceVM != null)
                {
                    ConnectedDevices.Remove(deviceVM);
                }
            }
        }
        private bool CanExecuteDisconnect()
        {
            return !string.IsNullOrEmpty(SelectedPort);
        }
        public bool IsPortConnected(string portName)
        {
            // Manager’daki ConnectedPorts içindeki SerialPort objelerinden
            // herhangi birinin PortName’i, aranan portName ile eşleşiyor mu?
            return _manager.ConnectedPorts.Any(sp => sp.PortName == portName);
        }

    }
}
