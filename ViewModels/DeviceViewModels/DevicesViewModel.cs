using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels
{
    public class DevicesViewModel : BindableBase
    {
        private readonly SerialPortsManager _manager;

        // Bağlı cihazların VM listesi
        public ObservableCollection<ConnectedDeviceViewModel> ConnectedDevices { get; }
            = new ObservableCollection<ConnectedDeviceViewModel>();

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

            ConnectCommand = new RelayCommand(ExecuteConnect, CanExecuteConnect);
            DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect);
        }
        private void OnMessageReceived(string portName, string data)
        {
            // UI thread'de çalışan kod
            Application.Current.Dispatcher.Invoke(() =>
            {
                var deviceVM = ConnectedDevices.FirstOrDefault(d => d.PortName == portName);
                if (deviceVM != null)
                {
                    deviceVM.Messages.Add(data);
                }
            });
        }

        private void ExecuteConnect()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                // Portu manager üzerinden aç
                _manager.ConnectToPort(SelectedPort, 921600);

                // Bağlandıysa, listemize ekle (yoksa)
                if (!ConnectedDevices.Any(d => d.PortName == SelectedPort))
                {
                    ConnectedDevices.Add(new ConnectedDeviceViewModel(_manager, SelectedPort));
                }
            }
        }
        private bool CanExecuteConnect() => !string.IsNullOrEmpty(SelectedPort);

        private void ExecuteDisconnect()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                // Manager'dan portu kapat
                _manager.DisconnectFromPort(SelectedPort);

                // Bağlılar listemizden de kaldırmak isterseniz:
                var deviceVM = ConnectedDevices.FirstOrDefault(d => d.PortName == SelectedPort);
                if (deviceVM != null)
                {
                    ConnectedDevices.Remove(deviceVM);
                }
            }
        }
        private bool CanExecuteDisconnect() => !string.IsNullOrEmpty(SelectedPort);

        public bool IsPortConnected(string portName)
        {
            return _manager.ConnectedPorts.Any(sp => sp.PortName == portName);
        }
    }
}
