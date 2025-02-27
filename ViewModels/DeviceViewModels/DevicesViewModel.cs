using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            // RelayCommand: parametresiz versiyon
            ConnectCommand = new RelayCommand(ExecuteConnect, CanExecuteConnect);
            DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect);
        }

        private void ExecuteConnect()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                // Dilediğiniz baud rate'i verin, örneğin 921600
                _manager.ConnectToPort(SelectedPort, 921600);
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
            }
        }
        private bool CanExecuteDisconnect()
        {
            return !string.IsNullOrEmpty(SelectedPort);
        }
    }
}
