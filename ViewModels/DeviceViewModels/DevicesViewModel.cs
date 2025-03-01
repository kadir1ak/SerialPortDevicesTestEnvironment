using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading.Tasks; // Task, async/await için
using SerialPortDevicesTestEnvironment.Helpers;
using SerialPortDevicesTestEnvironment.Models.Data;
using SerialPortDevicesTestEnvironment.Models.Device;
using SerialPortDevicesTestEnvironment.Services;

namespace SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels
{
    public class DevicesViewModel : BindableBase
    {
        private readonly SerialPortsManager _manager;

        // -- 1) Bağlı Cihazların Listesi --
        public ObservableCollection<Device> ConnectedDevices { get; } = new ObservableCollection<Device>();

        // -- 2) Manager'dan gelen port listeleri --
        public ObservableCollection<SerialPort> ConnectedPorts => _manager.ConnectedPorts;
        public ObservableCollection<string> AvailablePorts => _manager.AvailablePorts;

        // -- 3) Seçili Cihaz Nesnesi --
        private Device _device;
        public Device Device
        {
            get => _device;
            set
            {
                SetProperty(ref _device, value);
                // Command'ların tekrar CanExecute kontrolü yapmasını sağlamak için:
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // -- 4) UI’daki ComboBox’tan seçilen “PortName” --
        private string _selectedPortName;
        public string SelectedPortName
        {
            get => _selectedPortName;
            set
            {
                SetProperty(ref _selectedPortName, value);
                // Port adı değişince Connect butonunu aktif/pasif güncelle:
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // -- 5) Komutlar (Connect/Disconnect/SendMessage) --
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        // -- 6) Yapıcı Metot --
        public DevicesViewModel()
        {
            _manager = new SerialPortsManager();
            _manager.MessageReceived += OnMessageReceived;

            ConnectCommand = new RelayCommand(ExecuteConnect, CanExecuteConnect);
            DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect);
           
        }

        // ========== Gelen Veri Yakalama (Event) ==========
        private void OnMessageReceived(string portName, string data)
        {
            // Mesajları UI Thread üzerinde eklemek için:
            Application.Current.Dispatcher.Invoke(() =>
            {
                var device = ConnectedDevices.FirstOrDefault(d => d.PortName == portName);
                if (device != null)
                {
                    var newMessage = new Message
                    {
                        IncomingMessageIndex = device.Messages.Count,
                        IncomingMessage = data
                    };
                    device.Messages.Add(newMessage);
                }
            });
        }

        // ========== CONNECT ==========
        private void ExecuteConnect()
        {
            if (string.IsNullOrEmpty(SelectedPortName))
                return;

            // 1) Manager üzerinden ilgili portu aç
            _manager.ConnectToPort(SelectedPortName, 921600);

            // 2) ConnectedDevices içinde bu porta ait bir cihaz var mı?
            var existingDevice = ConnectedDevices.FirstOrDefault(d => d.PortName == SelectedPortName);
            if (existingDevice == null)
            {
                // Yoksa yeni bir device oluşturup ekleyin:
                var newDevice = new Device(_manager, SelectedPortName, isConnected:true);
                ConnectedDevices.Add(newDevice);
                Device = newDevice;
            }
            else
            {
                // Varsa yalnızca IsConnected durumunu güncelle
                existingDevice.IsConnected = true;
                Device = existingDevice;
            }
        }

        // Butonun aktif olması için: Sadece Port seçiliyse
        private bool CanExecuteConnect() => !string.IsNullOrEmpty(SelectedPortName);

        // ========== DISCONNECT ==========
        private async void ExecuteDisconnect()
        {
            // Seçili bir Device yoksa çık
            Device = ConnectedDevices.FirstOrDefault(d => d.PortName == SelectedPortName);
            if (Device == null)
                return;

            // PortName boşsa çık
            if (string.IsNullOrEmpty(Device.PortName))
                return;

            // Manager'dan portu kapat (ağır iş ise arka planda)
            await Task.Run(() => _manager.DisconnectFromPort(Device.PortName));

            // Device'i işaretle
            Device.IsConnected = false;

            // ConnectedDevices'tan çıkar
            var devToRemove = ConnectedDevices.FirstOrDefault(d => d.PortName == Device.PortName);
            if (devToRemove != null)
            {
                ConnectedDevices.Remove(devToRemove);
            }

            // Seçili cihazi null'la (UI'da buton vs. güncellenecek)
            Device = null;
        }

        // Butonun aktif olması için: Seçili bir Device ve Port’u dolu olmalı
        private bool CanExecuteDisconnect()
        {
            // Seçili port adına sahip, IsConnected=true durumda bir Device var mı?
            return !string.IsNullOrEmpty(SelectedPortName) && ConnectedDevices.Any(d => d.PortName == SelectedPortName && d.IsConnected);
        }

        // ========== Port Bağlı mı Kontrol ==========
        public bool IsPortConnected(string portName)
        {
            return _manager.ConnectedPorts.Any(sp => sp.PortName == portName);
        }
    }
}
